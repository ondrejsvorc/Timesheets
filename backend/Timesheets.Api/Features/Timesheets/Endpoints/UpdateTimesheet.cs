using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Domain;
using Timesheets.Api.Domain.Models;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Timesheets.Endpoints;

public sealed class UpdateTimesheet : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/{id}", Handle)
            .WithSummary("Update Timesheet")
            .WithRequestValidation<TimesheetEdit>();

    public sealed record Response(Guid Id, TimesheetEvaluation Evaluation);

    private static async Task<Results<Ok<Response>, NotFound, BadRequest<string>, ForbidHttpResult>> Handle(Guid id, [FromBody] TimesheetEdit request, AppDbContext dbContext, ICurrentUser user, TimesheetEvaluator evaluator, CancellationToken cancellationToken)
    {
        LoadedTimesheet? loaded = await LoadAsync(id, dbContext, cancellationToken);
        if (loaded is null)
        {
            return TypedResults.NotFound();
        }
        if ((!user.IsGlobalManagerRole() && user.EmployeeId != loaded.Timesheet.EmployeeId) || loaded.Timesheet.TimesheetStatus.Code != TimesheetStatus.DraftCode)
        {
            return TypedResults.Forbid();
        }
        if (evaluator.HasInactiveContractPartHours(loaded, request))
        {
            return TypedResults.BadRequest("Zakázkové hodiny nelze vyplnit mimo platnost pozice nebo projektu.");
        }
        ApplyEdits(loaded, request);
        TimesheetEvaluation evaluation = evaluator.Evaluate(loaded, request);
        await dbContext.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(new Response(id, evaluation));
    }

    private static async Task<LoadedTimesheet?> LoadAsync(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Domain.Models.Timesheet? timesheet = await dbContext.Timesheets
            .Include(value => value.Employee)
            .Include(value => value.TimesheetStatus)
            .SingleOrDefaultAsync(value => value.Id == id, cancellationToken);

        if (timesheet is null)
        {
            return null;
        }

        Domain.Models.Attendance? attendance = await dbContext.Attendances
            .Include(value => value.Days)
            .SingleOrDefaultAsync(value => value.TimesheetId == id, cancellationToken);

        if (attendance is null)
        {
            return null;
        }

        List<Domain.Models.ContractPart> projects = await dbContext.ContractParts
            .Include(value => value.Days)
            .Where(value => value.TimesheetId == timesheet.Id)
            .ToListAsync(cancellationToken);

        Guid[] assignmentIds = projects.Select(project => project.ContractEmployeeId).ToArray();
        var rangeRows = await (
            from assignment in dbContext.ContractEmployees.AsNoTracking()
            join contract in dbContext.Contracts.AsNoTracking() on assignment.ContractId equals contract.Id
            join project in dbContext.Projects.AsNoTracking() on contract.ProjectId equals project.Id
            where assignmentIds.Contains(assignment.Id)
            select new
            {
                assignment.Id,
                assignment.StartDate,
                AssignmentEndDate = assignment.EndDate,
                ProjectStartDate = project.StartDate,
                ProjectEndDate = project.EndDate
            })
            .ToListAsync(cancellationToken);
        Dictionary<Guid, ContractPartDateRange> projectRanges = rangeRows.ToDictionary(
            row => row.Id,
            row => EffectiveContractPartRange(row.StartDate, row.AssignmentEndDate, row.ProjectStartDate, row.ProjectEndDate));

        decimal totalWorkload = await GetWorkloadAsync(timesheet.EmployeeId, timesheet.Year, timesheet.Month, dbContext, cancellationToken);
        decimal coreWorkload = Math.Max(0m, totalWorkload - projects.Sum(project => project.Workload));
        return new LoadedTimesheet(Timesheet: timesheet, Attendance: attendance, ContractParts: projects, ContractPartRanges: projectRanges, TotalWorkload: totalWorkload, CoreWorkload: coreWorkload);
    }

    internal static void ApplyEdits(LoadedTimesheet loaded, TimesheetEdit request)
    {
        Dictionary<DateOnly, Domain.Models.AttendanceDay> days = loaded.Attendance.Days.ToDictionary(day => DateOnly.FromDateTime(day.Date));
        foreach (DayEdit update in request.Days)
        {
            if (!days.TryGetValue(DateOnly.FromDateTime(update.Date), out Domain.Models.AttendanceDay? day))
            {
                continue;
            }

            day.ClockIn = update.ClockIn;
            day.ClockOut = update.ClockOut;
            day.BreakStart = update.BreakStart;
            day.BreakEnd = update.BreakEnd;
            day.CoreHours = TimesheetEvaluator.Normalize(update.CoreHours);
            day.Description = update.Description;
            day.Schedules = JsonSerializer.Serialize(update.Schedules ?? []);
            day.HoursWithoutBreak = TimesheetEvaluator.CalculateWorkedHoursFromAttendance(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd);
        }

        Dictionary<Guid, ContractPartEdit> projects = (request.ContractParts ?? []).ToDictionary(project => project.ContractEmployeeId);
        foreach (Domain.Models.ContractPart project in loaded.ContractParts)
        {
            if (loaded.ContractPartRanges.TryGetValue(project.ContractEmployeeId, out ContractPartDateRange? range))
            {
                foreach (Domain.Models.ContractPartDay day in project.Days.Where(day => !range.Includes(day.Date)))
                {
                    day.Hours = 0m;
                    day.HoursLocked = false;
                }
            }

            if (!projects.TryGetValue(project.ContractEmployeeId, out ContractPartEdit? update))
            {
                continue;
            }

            project.UpdatedAt = DateTime.UtcNow;
            if (project.LockedAt is not null)
            {
                continue;
            }

            Dictionary<DateOnly, Domain.Models.ContractPartDay> contractPartDays = project.Days.ToDictionary(day => DateOnly.FromDateTime(day.Date));

            foreach (ContractPartDayEdit contractPartDay in update.Days)
            {
                if (contractPartDays.TryGetValue(DateOnly.FromDateTime(contractPartDay.Date), out Domain.Models.ContractPartDay? day))
                {
                    bool active = loaded.ContractPartRanges.TryGetValue(project.ContractEmployeeId, out range) && range.Includes(contractPartDay.Date);
                    day.Hours = active ? TimesheetEvaluator.Normalize(contractPartDay.Hours) : 0m;
                    day.HoursLocked = active && contractPartDay.HoursLocked;
                }
            }
        }

        loaded.Timesheet.UpdatedAt = DateTime.UtcNow;
    }

    private static async Task<decimal> GetWorkloadAsync(Guid employeeId, int year, int month, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        decimal? monthly = await dbContext.EmployeeWorkloads
            .AsNoTracking()
            .Where(workload => workload.EmployeeId == employeeId && workload.Year == year && workload.Month == month)
            .Select(workload => (decimal?)workload.Workload)
            .FirstOrDefaultAsync(cancellationToken);

        if (monthly.HasValue)
        {
            return monthly.Value;
        }

        DateTime periodStart = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime periodEnd = periodStart.AddMonths(1).AddDays(-1);

        return await dbContext.CoreEmployments
            .AsNoTracking()
            .Where(employment => employment.EmployeeId == employeeId)
            .Where(employment => employment.StartDate <= periodEnd && (employment.EndDate == null || employment.EndDate >= periodStart))
            .OrderByDescending(employment => employment.StartDate)
            .Select(employment => (decimal?)employment.Workload)
            .FirstOrDefaultAsync(cancellationToken) ?? 0m;
    }

    private static ContractPartDateRange EffectiveContractPartRange(DateTime assignmentStartDate, DateTime? assignmentEndDate, DateTime projectStartDate, DateTime? projectEndDate)
    {
        DateTime start = Max(ToUtcDate(assignmentStartDate), ToUtcDate(projectStartDate));
        DateTime? end = Min(assignmentEndDate.HasValue ? ToUtcDate(assignmentEndDate.Value) : null, projectEndDate.HasValue ? ToUtcDate(projectEndDate.Value) : null);
        return new ContractPartDateRange(start, end);
    }

    private static DateTime Max(DateTime first, DateTime second) => first >= second ? first : second;

    private static DateTime? Min(DateTime? first, DateTime? second) => (first, second) switch
    {
        (null, null) => null,
        (DateTime value, null) => value,
        (null, DateTime value) => value,
        (DateTime left, DateTime right) => left <= right ? left : right
    };

    private static DateTime ToUtcDate(DateTime value) => value.Kind == DateTimeKind.Utc ? value.Date : DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
}
