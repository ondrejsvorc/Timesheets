using CzechHolidays;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;
using Timesheets.Api.Domain.Models;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Timesheets.Endpoints;

public sealed class GetTimesheetOverview : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/overview", Handle)
           .WithSummary("Get Timesheet Overview");

    public sealed record Request([FromQuery] Guid EmployeeId, [FromQuery] int Year, [FromQuery] int Month);

    public sealed record OverviewItem(
        Guid? TimesheetId,
        string Kind,
        string Label,
        string? ContractRegistrationNumber,
        string? Position,
        decimal Workload,
        IEnumerable<string> Managers,
        string Status,
        string StatusCode,
        TimesheetContractPartActions? Actions,
        Guid? ContractId,
        Guid? ProjectId);

    public sealed record MonthSummary(DateTime PeriodStart, DateTime PeriodEnd, int Workdays, int VacationDays, int SickDays, int Holidays, decimal TotalWorkload);

    public sealed record Response(Guid EmployeeId, int Year, int Month, string Status, string StatusCode, TimesheetMonthActions Actions, IEnumerable<OverviewItem> Items, MonthSummary Summary);

    private sealed record SummaryDay(DateTime Date, bool IsHoliday, string? Description);

    private sealed record ContractPartOverviewRow(
        Guid TimesheetId,
        Guid ContractId,
        Guid ProjectId,
        string ContractRegistrationNumber,
        string Position,
        decimal Workload,
        string StatusCode);

    private static async Task<Results<Ok<Response>, NotFound, ForbidHttpResult>> Handle(
        [AsParameters] Request request,
        AppDbContext dbContext,
        ICzechHolidaysFactory holidaysFactory,
        ICurrentUser user,
        CancellationToken cancellationToken)
    {
        if (!await user.CanAccessEmployeeAsync(request.EmployeeId, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var attendanceInfo = await (
            from timesheet in dbContext.Timesheets.AsNoTracking()
            join attendance in dbContext.Attendances.AsNoTracking() on timesheet.Id equals attendance.TimesheetId
            where timesheet.EmployeeId == request.EmployeeId && timesheet.Year == request.Year && timesheet.Month == request.Month
            select new
            {
                timesheet.Id,
                Status = timesheet.TimesheetStatus.Name,
                StatusCode = timesheet.TimesheetStatus.Code,
                Days = attendance.Days
                    .OrderBy(day => day.Date)
                    .Select(day => new SummaryDay(day.Date, day.IsHoliday, day.Description))
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (attendanceInfo is null)
        {
            return TypedResults.NotFound();
        }

        await EnsureContractPartsForEmployeeMonthAsync(request.EmployeeId, request.Year, request.Month, dbContext, holidaysFactory, cancellationToken);

        List<ContractPartOverviewRow> contractPartRows = await dbContext.ContractParts
            .AsNoTracking()
            .Where(part => part.TimesheetId == attendanceInfo.Id)
            .Join(
                dbContext.ContractEmployees.AsNoTracking(),
                part => part.ContractEmployeeId,
                contractEmployee => contractEmployee.Id,
                (part, contractEmployee) => new { part, contractEmployee })
            .Join(
                dbContext.Contracts.AsNoTracking(),
                x => x.contractEmployee.ContractId,
                contract => contract.Id,
                (x, contract) => new ContractPartOverviewRow(
                    x.part.Id,
                    contract.Id,
                    contract.ProjectId,
                    contract.RegistrationNumber,
                    x.contractEmployee.Position,
                    x.part.Workload,
                    x.part.TimesheetStatus.Code))
            .ToListAsync(cancellationToken);

        HashSet<DateOnly> holidays = holidaysFactory.Create(request.Year).Select(holiday => holiday.Date).ToHashSet();
        List<SummaryDay> summaryDays = attendanceInfo.Days
            .Select(day => day with { IsHoliday = day.IsHoliday || holidays.Contains(DateOnly.FromDateTime(day.Date)) })
            .ToList();

        decimal totalProjectWorkload = contractPartRows.Sum(item => item.Workload);
        decimal totalWorkload = await GetEmployeeWorkloadAsync(request.EmployeeId, request.Year, request.Month, dbContext, cancellationToken);
        decimal coreWorkload = Math.Max(0m, totalWorkload - totalProjectWorkload);
        MonthSummary summary = ComputeMonthSummary(request.Year, request.Month, summaryDays, totalWorkload);
        bool allContractPartsApproved = contractPartRows.Count == 0 || contractPartRows.All(row => row.StatusCode == TimesheetStatus.ApprovedCode);
        TimesheetMonthActions monthActions = TimesheetActions.ResolveMonthActions(user, request.EmployeeId, attendanceInfo.StatusCode, allContractPartsApproved);

        List<OverviewItem> items =
        [
            new(attendanceInfo.Id, "core", "Kmen", null, null, coreWorkload, [], attendanceInfo.Status, attendanceInfo.StatusCode, null, null, null),
        ];

        Guid[] contractIds = contractPartRows.Select(row => row.ContractId).Distinct().ToArray();
        var managerRows = await dbContext.ContractManagers
            .AsNoTracking()
            .Include(manager => manager.Employee)
            .Where(manager => contractIds.Contains(manager.ContractId))
            .OrderBy(manager => manager.Employee.Surname)
            .ThenBy(manager => manager.Employee.FirstName)
            .Select(manager => new { manager.ContractId, manager.Employee.DisplayName })
            .ToListAsync(cancellationToken);
        ILookup<Guid, string> managersByContract = managerRows.ToLookup(manager => manager.ContractId, manager => manager.DisplayName);

        for (int index = 0; index < contractPartRows.Count; index++)
        {
            ContractPartOverviewRow row = contractPartRows[index];
            items.Add(new OverviewItem(
                row.TimesheetId,
                "contractPart",
                $"Projektová činnost {index + 1}",
                row.ContractRegistrationNumber,
                row.Position,
                row.Workload,
                managersByContract[row.ContractId],
                TimesheetStatus.ResolveContractPartDisplayStatus(row.StatusCode),
                row.StatusCode,
                TimesheetActions.ResolveContractPartActions(user, attendanceInfo.StatusCode, row.StatusCode, row.ContractId, row.ProjectId),
                row.ContractId,
                row.ProjectId));
        }

        return TypedResults.Ok(new Response(request.EmployeeId, request.Year, request.Month, attendanceInfo.Status, attendanceInfo.StatusCode, monthActions, items, summary));
    }

    private static async Task EnsureContractPartsForEmployeeMonthAsync(Guid employeeId, int year, int month, AppDbContext dbContext, ICzechHolidaysFactory holidaysFactory, CancellationToken cancellationToken)
    {
        DateTime periodStart = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime periodEnd = periodStart.AddMonths(1).AddDays(-1);
        List<ContractEmployee> assignments = await dbContext.ContractEmployees
            .AsNoTracking()
            .Include(assignment => assignment.Contract)
            .ThenInclude(contract => contract.Project)
            .Where(assignment => assignment.EmployeeId == employeeId && assignment.StartDate <= periodEnd && (!assignment.EndDate.HasValue || assignment.EndDate >= periodStart))
            .Where(assignment => !assignment.Contract.Project.EndDate.HasValue || assignment.Contract.Project.EndDate >= periodStart)
            .ToListAsync(cancellationToken);

        if (assignments.Count == 0)
        {
            return;
        }

        Guid timesheetId = await TimesheetBootstrap.EnsureMonthTimesheetIdAsync(dbContext, employeeId, year, month, cancellationToken);
        Guid[] assignmentIds = assignments.Select(assignment => assignment.Id).ToArray();
        HashSet<Guid> existingAssignmentIds = await dbContext.ContractParts
            .AsNoTracking()
            .Where(part => part.TimesheetId == timesheetId && assignmentIds.Contains(part.ContractEmployeeId))
            .Select(part => part.ContractEmployeeId)
            .ToHashSetAsync(cancellationToken);
        List<ContractEmployee> missingAssignments = assignments.Where(assignment => !existingAssignmentIds.Contains(assignment.Id)).ToList();

        if (missingAssignments.Count == 0)
        {
            return;
        }

        HashSet<DateOnly> holidays = holidaysFactory.Create(year).Select(holiday => holiday.Date).ToHashSet();
        dbContext.ContractParts.AddRange(missingAssignments.Select(assignment => CreateContractPart(assignment, year, month, holidays, timesheetId)));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ContractPart CreateContractPart(ContractEmployee assignment, int year, int month, HashSet<DateOnly> holidays, Guid timesheetId)
    {
        ContractPart contractPart = new()
        {
            Id = Guid.CreateVersion7(),
            TimesheetId = timesheetId,
            ContractEmployeeId = assignment.Id,
            TimesheetStatusId = TimesheetStatus.DraftId,
            Workload = assignment.Workload,
            CreatedAt = DateTime.UtcNow,
        };

        ContractPartDateRange range = EffectiveContractPartRange(
            assignment.StartDate,
            assignment.EndDate,
            assignment.Contract?.Project?.StartDate ?? assignment.StartDate,
            assignment.Contract?.Project?.EndDate);

        for (int day = 1; day <= DateTime.DaysInMonth(year, month); day++)
        {
            DateTime date = new(year, month, day, 0, 0, 0, DateTimeKind.Utc);
            if (!range.Includes(date))
            {
                continue;
            }

            bool isHoliday = holidays.Contains(DateOnly.FromDateTime(date));
            contractPart.Days.Add(new ContractPartDay
            {
                Id = Guid.CreateVersion7(),
                ContractPartId = contractPart.Id,
                Date = date,
                Hours = 0m,
                IsHoliday = isHoliday,
                HoursObligation = TimesheetEvaluator.CalculateTotalHoursObligation(date, isHoliday, assignment.Workload),
            });
        }

        return contractPart;
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

    private static MonthSummary ComputeMonthSummary(int year, int month, IReadOnlyList<SummaryDay> days, decimal totalWorkload)
    {
        DateTime periodStart = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime periodEnd = periodStart.AddMonths(1).AddDays(-1);
        HashSet<string> vacationCodes = ["D"];
        HashSet<string> sickCodes = ["N", "NL", "NP", "O", "ZV"];

        int workdays = days.Count(day => TimesheetEvaluator.IsWorkday(day.Date, day.IsHoliday));
        int vacationDays = days.Count(day => HasInterruptionCode(day.Description, vacationCodes));
        int sickDays = days.Count(day => HasInterruptionCode(day.Description, sickCodes));
        int holidayCount = days.Count(day => day.IsHoliday);

        return new MonthSummary(periodStart, periodEnd, workdays, vacationDays, sickDays, holidayCount, totalWorkload);
    }

    private static bool HasInterruptionCode(string? raw, HashSet<string> codes)
    {
        foreach (string code in ParseInterruptionCodes(raw))
        {
            if (codes.Contains(code))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> ParseInterruptionCodes(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            yield break;
        }

        foreach (string part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (string token in part.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                string code = token.Split('(')[0].ToUpperInvariant();
                if (code.Length > 0 && code.All(char.IsLetter))
                {
                    yield return code;
                }
            }
        }
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
