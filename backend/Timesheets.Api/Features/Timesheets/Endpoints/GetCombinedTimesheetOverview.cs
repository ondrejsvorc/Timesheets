using CzechHolidays;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Features.Auth;
using Timesheets.Api.Features.Timesheets;

namespace Timesheets.Api.Features.Timesheets.Endpoints;

public sealed class GetCombinedTimesheetOverview : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/combined/overview", Handle)
           .WithSummary("Get Combined Timesheet Overview");

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
        Guid? ContractId,
        Guid? ProjectId);
    public sealed record Response(Guid EmployeeId, int Year, int Month, string Status, IEnumerable<OverviewItem> Items, TimesheetMonthSummary Summary);
    private sealed record ProjectRowSource(
        Guid TimesheetId,
        Guid ContractId,
        Guid ProjectId,
        string ContractRegistrationNumber,
        string Position,
        decimal Workload,
        Guid TimesheetStatusId);
    private sealed record ManagerRowSource(Guid ContractId, string FullName);

    private static async Task<Results<Ok<Response>, NotFound, ForbidHttpResult>> Handle([AsParameters] Request request, AppDbContext dbContext, ICzechHolidaysFactory holidaysFactory, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!await user.CanAccessEmployeeAsync(request.EmployeeId, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var attendanceInfo = await dbContext.AttendanceTimesheets
            .AsNoTracking()
            .Where(t => t.EmployeeId == request.EmployeeId && t.Year == request.Year && t.Month == request.Month)
            .Select(t => new
            {
                t.Id,
                Status = t.TimesheetStatus.Name,
                Days = t.Days
                    .OrderBy(day => day.Date)
                    .Select(day => new TimesheetMonthSummaryDay(day.Date, day.IsHoliday, day.Description))
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (attendanceInfo is null)
        {
            return TypedResults.NotFound();
        }

        await ProjectTimesheetInitializer.EnsureForEmployeeMonthAsync(request.EmployeeId, request.Year, request.Month, dbContext, holidaysFactory, cancellationToken);

        List<ProjectRowSource> projectRows = await dbContext.ProjectTimesheets
            .AsNoTracking()
            .Where(timesheet => timesheet.EmployeeId == request.EmployeeId && timesheet.Year == request.Year && timesheet.Month == request.Month)
            .Join(
                dbContext.ContractEmployees.AsNoTracking(),
                timesheet => timesheet.ContractEmployeeId,
                contractEmployee => contractEmployee.Id,
                (timesheet, contractEmployee) => new { timesheet, contractEmployee })
            .Join(
                dbContext.Contracts.AsNoTracking(),
                x => x.contractEmployee.ContractId,
                contract => contract.Id,
                (x, contract) => new ProjectRowSource(
                    x.timesheet.Id,
                    contract.Id,
                    contract.ProjectId,
                    contract.RegistrationNumber,
                    x.contractEmployee.Position,
                    x.timesheet.Workload,
                    x.timesheet.TimesheetStatusId))
            .ToListAsync(cancellationToken);

        HashSet<DateOnly> holidays = holidaysFactory.Create(request.Year).Select(holiday => holiday.Date).ToHashSet();
        List<TimesheetMonthSummaryDay> summaryDays = attendanceInfo.Days
            .Select(day => day with { IsHoliday = day.IsHoliday || holidays.Contains(DateOnly.FromDateTime(day.Date)) })
            .ToList();

        decimal totalProjectWorkload = projectRows.Sum(item => item.Workload);
        decimal totalWorkload = await TimesheetWorkloads.GetAsync(request.EmployeeId, request.Year, request.Month, dbContext, cancellationToken);
        decimal coreWorkload = Math.Max(0m, totalWorkload - totalProjectWorkload);
        TimesheetMonthSummary summary = TimesheetMonthSummaryCalculator.Compute(request.Year, request.Month, summaryDays, totalWorkload);

        List<OverviewItem> items =
        [
            new(attendanceInfo.Id, "core", "Kmen", null, null, coreWorkload, [], attendanceInfo.Status, null, null),
        ];

        Guid[] contractIds = projectRows.Select(row => row.ContractId).Distinct().ToArray();
        List<ManagerRowSource> managerRows = await dbContext.ContractManagers
            .AsNoTracking()
            .Where(manager => contractIds.Contains(manager.ContractId))
            .OrderBy(manager => manager.Employee.FullName)
            .Select(manager => new ManagerRowSource(manager.ContractId, manager.Employee.FullName))
            .ToListAsync(cancellationToken);
        ILookup<Guid, string> managersByContract = managerRows.ToLookup(manager => manager.ContractId, manager => manager.FullName);

        for (int index = 0; index < projectRows.Count; index++)
        {
            ProjectRowSource row = projectRows[index];
            string projectStatus = TimesheetWorkflow.ResolveProjectDisplayStatus(row.TimesheetStatusId);

            items.Add(new OverviewItem(
                row.TimesheetId,
                "project",
                $"Projektová činnost {index + 1}",
                row.ContractRegistrationNumber,
                row.Position,
                row.Workload,
                managersByContract[row.ContractId],
                projectStatus,
                row.ContractId,
                row.ProjectId
            ));
        }

        return TypedResults.Ok(new Response(request.EmployeeId, request.Year, request.Month, attendanceInfo.Status, items, summary));
    }
}
