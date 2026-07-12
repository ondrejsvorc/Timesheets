using System.Text.Json;
using CzechHolidays;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;
using Timesheets.Api.Domain.Models;
using Timesheets.Api.Features.Auth;
using Timesheets.Api.Features.Employees;

namespace Timesheets.Api.Features.Timesheets.Endpoints;

public sealed class GetTimesheet : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/", Handle)
           .WithSummary("Get Timesheet");

    public sealed record Request([FromQuery] Guid EmployeeId, [FromQuery] int Year, [FromQuery] int Month);
    public sealed record ContractPartDefinition(string Id, string RegistrationNumber, string Name, string Position, decimal Workload, bool Locked, bool[] ActiveDays);
    public sealed record ContractPartCell(decimal Hours, bool Locked);
    public sealed record DayItem(int Day, int?[] Work, int?[] Break, decimal CoreHours, ContractPartCell[] ContractPartCells, bool IsHoliday, bool IsWeekend, string? Note, IReadOnlyList<int[]>? Schedules);
    public sealed record Response(Guid Id, int Year, int Month, decimal TotalWorkload, decimal CoreWorkload, bool TracksAttendance, IEnumerable<ContractPartDefinition> ContractParts, IEnumerable<DayItem> Days);

    private sealed record AttendanceDaySource(DateTime Date, TimeSpan? ClockIn, TimeSpan? ClockOut, TimeSpan? BreakStart, TimeSpan? BreakEnd, decimal Workload, decimal HoursWithoutBreak, decimal CoreHours, bool IsHoliday, string? Description, string Schedules);
    private sealed record ContractPartDaySource(DateTime Date, decimal Hours, bool HoursLocked, bool IsHoliday);
    private sealed record ContractPartSource(Guid ActivityId, Guid ProjectId, string RegistrationNumber, string ProjectName, string Position, decimal Workload, DateTime? LockedAt, ContractPartDateRange Range, List<ContractPartDaySource> Days);
    private sealed record ContractPartRow(Guid ActivityId, Guid ProjectId, string RegistrationNumber, string ProjectName, string Position, decimal Workload, DateTime? LockedAt, DateTime AssignmentStartDate, DateTime? AssignmentEndDate, DateTime ProjectStartDate, DateTime? ProjectEndDate, List<ContractPartDaySource> Days);

    private static async Task<Results<Ok<Response>, NotFound, ForbidHttpResult>> Handle([AsParameters] Request request, AppDbContext dbContext, ICzechHolidaysFactory holidaysFactory, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!await user.CanAccessEmployeeAsync(request.EmployeeId, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var attendanceTimesheet = await (
            from timesheet in dbContext.Timesheets.AsNoTracking()
            join attendance in dbContext.Attendances.AsNoTracking() on timesheet.Id equals attendance.TimesheetId
            where timesheet.EmployeeId == request.EmployeeId && timesheet.Year == request.Year && timesheet.Month == request.Month
            select new
            {
                timesheet.Id,
                EmployeeTypeId = attendance.EmployeeTypeId,
                Days = attendance.Days.Select(d => new AttendanceDaySource(d.Date, d.ClockIn, d.ClockOut, d.BreakStart, d.BreakEnd, d.Workload, d.HoursWithoutBreak, d.CoreHours, d.IsHoliday, d.Description, d.Schedules)).ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (attendanceTimesheet is null)
        {
            return TypedResults.NotFound();
        }

        List<ContractPartRow> contractPartRows = await (
            from part in dbContext.ContractParts.AsNoTracking()
            join contractEmployee in dbContext.ContractEmployees.AsNoTracking() on part.ContractEmployeeId equals contractEmployee.Id
            join contract in dbContext.Contracts.AsNoTracking() on contractEmployee.ContractId equals contract.Id
            join project in dbContext.Projects.AsNoTracking() on contract.ProjectId equals project.Id
            where part.TimesheetId == attendanceTimesheet.Id
            select new ContractPartRow(
                contractEmployee.Id,
                project.Id,
                contract.RegistrationNumber,
                project.Name,
                contractEmployee.Position,
                part.Workload,
                part.LockedAt,
                contractEmployee.StartDate,
                contractEmployee.EndDate,
                project.StartDate,
                project.EndDate,
                part.Days.Select(d => new ContractPartDaySource(d.Date, d.Hours, d.HoursLocked, d.IsHoliday)).ToList()
            )
        ).ToListAsync(cancellationToken);

        List<ContractPartSource> contractParts = contractPartRows
            .Select(row => new ContractPartSource(
                row.ActivityId,
                row.ProjectId,
                row.RegistrationNumber,
                row.ProjectName,
                row.Position,
                row.Workload,
                row.LockedAt,
                EffectiveContractPartRange(row.AssignmentStartDate, row.AssignmentEndDate, row.ProjectStartDate, row.ProjectEndDate),
                row.Days))
            .ToList();

        decimal totalProjectWorkload = contractParts.Sum(t => t.Workload);
        decimal totalWorkload = await GetEmployeeWorkloadAsync(request.EmployeeId, request.Year, request.Month, dbContext, cancellationToken);
        decimal coreWorkload = Math.Max(0m, totalWorkload - totalProjectWorkload);
        bool tracksAttendance = EmployeeTypes.TracksAttendance(attendanceTimesheet.EmployeeTypeId);
        List<ContractPartColumn> contractPartStates = contractParts
            .Select(t => new ContractPartColumn(t.ActivityId, t.Workload, t.LockedAt is not null, t.Range))
            .ToList();
        List<ContractPartDefinition> projects = contractParts
            .Select(t => new ContractPartDefinition(
                t.ActivityId.ToString(),
                t.RegistrationNumber,
                t.ProjectName,
                t.Position,
                t.Workload,
                t.LockedAt is not null,
                BuildActiveDays(request.Year, request.Month, t.Range)
            ))
            .OrderBy(p => p.Name)
            .ToList();

        Dictionary<string, int> contractPartIndexById = projects
            .Select((p, index) => new { p.Id, Index = index })
            .ToDictionary(x => x.Id, x => x.Index);

        Dictionary<DateOnly, Dictionary<string, ContractPartCell>> contractPartCellsByDate = contractParts
            .SelectMany(timesheet => timesheet.Days.Where(day => timesheet.Range.Includes(day.Date)).Select(day => new
            {
                Date = DateOnly.FromDateTime(day.Date),
                timesheet.ActivityId,
                day.Hours,
                day.HoursLocked
            }))
            .GroupBy(item => item.Date)
            .ToDictionary(
                group => group.Key,
                group => group
                    .GroupBy(item => item.ActivityId.ToString())
                    .ToDictionary(projectGroup => projectGroup.Key, projectGroup => new ContractPartCell(
                        Hours: projectGroup.Sum(item => item.Hours),
                        Locked: projectGroup.Any(item => item.HoursLocked)))
            );

        HashSet<DateOnly> holidays = holidaysFactory.Create(request.Year).Select(holiday => holiday.Date).ToHashSet();
        Dictionary<DateOnly, bool> holidayByDate = Enumerable.Range(1, DateTime.DaysInMonth(request.Year, request.Month))
            .Select(day => DateOnly.FromDateTime(new DateTime(request.Year, request.Month, day, 0, 0, 0, DateTimeKind.Utc)))
            .ToDictionary(date => date, date => holidays.Contains(date));

        foreach (AttendanceDaySource attendanceDay in attendanceTimesheet.Days)
        {
            DateOnly date = DateOnly.FromDateTime(attendanceDay.Date);
            holidayByDate[date] = attendanceDay.IsHoliday || holidayByDate.GetValueOrDefault(date);
        }

        foreach (ContractPartDaySource contractPartDay in contractParts.SelectMany(timesheet => timesheet.Days))
        {
            DateOnly date = DateOnly.FromDateTime(contractPartDay.Date);
            holidayByDate[date] = contractPartDay.IsHoliday || holidayByDate.GetValueOrDefault(date);
        }

        Dictionary<DateOnly, AttendanceDaySource> attendanceDaysByDate = attendanceTimesheet.Days
            .ToDictionary(day => DateOnly.FromDateTime(day.Date), day => day);

        List<DayItem> days = Enumerable.Range(1, DateTime.DaysInMonth(request.Year, request.Month))
            .Select(dayNumber =>
            {
                DateTime date = new(request.Year, request.Month, dayNumber, 0, 0, 0, DateTimeKind.Utc);
                DateOnly dateOnly = DateOnly.FromDateTime(date);
                AttendanceDaySource? attendanceDay = attendanceDaysByDate.GetValueOrDefault(dateOnly);
                Dictionary<string, ContractPartCell> projectCells = contractPartCellsByDate.GetValueOrDefault(dateOnly) ?? [];
                ContractPartCell[] contractPartCellsArray = Enumerable.Repeat(new ContractPartCell(0m, false), projects.Count).ToArray();
                foreach ((string contractEmployeeId, ContractPartCell cell) in projectCells)
                {
                    if (contractPartIndexById.TryGetValue(contractEmployeeId, out int index))
                    {
                        contractPartCellsArray[index] = cell;
                    }
                }

                decimal coreHours = attendanceDay?.CoreHours ?? 0m;
                if (attendanceDay is not null && !string.IsNullOrWhiteSpace(attendanceDay.Description))
                {
                    EditableTimesheetDay dayState = new()
                    {
                        Date = date,
                        ClockIn = attendanceDay.ClockIn,
                        ClockOut = attendanceDay.ClockOut,
                        BreakStart = attendanceDay.BreakStart,
                        BreakEnd = attendanceDay.BreakEnd,
                        Description = attendanceDay.Description,
                        Schedules = [],
                        IsHoliday = holidayByDate.GetValueOrDefault(dateOnly),
                        CoreHours = coreHours,
                        CoreHoursFixed = false,
                        ContractPartHours = projects.ToDictionary(project => Guid.Parse(project.Id), project => contractPartCellsArray[contractPartIndexById[project.Id]].Hours),
                        ContractPartHoursFixed = projects.ToDictionary(project => Guid.Parse(project.Id), project => contractPartCellsArray[contractPartIndexById[project.Id]].Locked),
                        ContractPartHoursFloor = projects.ToDictionary(
                            project => Guid.Parse(project.Id),
                            project =>
                            {
                                ContractPartCell cell = contractPartCellsArray[contractPartIndexById[project.Id]];
                                return cell.Locked && cell.Hours > 0m ? cell.Hours : 0m;
                            })
                    };
                    TimesheetEvaluator.ApplyInterruptionToDayState(dayState, contractPartStates, totalWorkload, tracksAttendance);
                    coreHours = dayState.CoreHours;
                    foreach (ContractPartDefinition project in projects)
                    {
                        int projectIndex = contractPartIndexById[project.Id];
                        contractPartCellsArray[projectIndex] = contractPartCellsArray[projectIndex] with { Hours = dayState.ContractPartHours[Guid.Parse(project.Id)] };
                    }
                }

                return new DayItem(
                    dayNumber,
                    [ToMinutes(attendanceDay?.ClockIn), ToMinutes(attendanceDay?.ClockOut)],
                    [ToMinutes(attendanceDay?.BreakStart), ToMinutes(attendanceDay?.BreakEnd)],
                    coreHours,
                    contractPartCellsArray,
                    holidayByDate.GetValueOrDefault(dateOnly),
                    date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
                    string.IsNullOrWhiteSpace(attendanceDay?.Description) ? null : attendanceDay.Description,
                    ParseSchedules(attendanceDay?.Schedules)
                );
            })
            .ToList();

        return TypedResults.Ok(new Response(attendanceTimesheet.Id, request.Year, request.Month, totalWorkload, coreWorkload, tracksAttendance, projects, days));
    }

    private static async Task<decimal> GetEmployeeWorkloadAsync(Guid employeeId, int year, int month, AppDbContext dbContext, CancellationToken cancellationToken)
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

    private static int? ToMinutes(TimeSpan? value) => value.HasValue ? (int)Math.Round(value.Value.TotalMinutes) : null;

    private static bool[] BuildActiveDays(int year, int month, ContractPartDateRange range) => Enumerable.Range(1, DateTime.DaysInMonth(year, month))
        .Select(day => range.Includes(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc)))
        .ToArray();

    private static IReadOnlyList<int[]>? ParseSchedules(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            List<JsonTimeRange>? ranges = JsonSerializer.Deserialize<List<JsonTimeRange>>(value);
            if (ranges is null || ranges.Count == 0)
            {
                return null;
            }

            List<int[]> parsed = [];
            foreach (JsonTimeRange range in ranges)
            {
                if (!TryParseMinutes(range.Start, out int start) || !TryParseMinutes(range.End, out int end))
                {
                    continue;
                }

                parsed.Add([start, end]);
            }

            return parsed.Count > 0 ? parsed : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool TryParseMinutes(string value, out int minutes)
    {
        minutes = 0;
        if (!TimeSpan.TryParse(value, out TimeSpan parsed))
        {
            return false;
        }

        minutes = (int)Math.Round(parsed.TotalMinutes);
        return true;
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

    private sealed record JsonTimeRange(string Start, string End);
}
