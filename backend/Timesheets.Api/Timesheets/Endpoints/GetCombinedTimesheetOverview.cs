using CzechHolidays;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Auth;
using Timesheets.Api.Data;
using Timesheets.Api.Timesheets;

namespace Timesheets.Api.Timesheets.Endpoints;

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
        string? ContractName,
        string? Position,
        decimal Workload,
        IEnumerable<string> Managers,
        string Status,
        Guid? ContractId,
        Guid? ProjectId);
    public sealed record Response(Guid EmployeeId, int Year, int Month, string Status, IEnumerable<OverviewItem> Items);
    private sealed record ProjectRowSource(
        Guid TimesheetId,
        Guid ContractId,
        Guid ProjectId,
        string ContractName,
        string Position,
        decimal Workload,
        Guid TimesheetStatusId);

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
                Days = t.Days.Select(d => d.Workload).ToList()
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
                    contract.Name,
                    x.contractEmployee.Position,
                    x.timesheet.Workload,
                    x.timesheet.TimesheetStatusId))
            .ToListAsync(cancellationToken);

        decimal totalProjectWorkload = projectRows.Sum(item => item.Workload);
        decimal totalWorkload = await TimesheetWorkloads.GetAsync(request.EmployeeId, request.Year, request.Month, dbContext, cancellationToken);
        decimal coreWorkload = Math.Max(0m, totalWorkload - totalProjectWorkload);

        List<OverviewItem> items =
        [
            new(attendanceInfo.Id, "core", "Kmenový úvazek", null, null, coreWorkload, [], attendanceInfo.Status, null, null),
        ];

        for (int index = 0; index < projectRows.Count; index++)
        {
            ProjectRowSource row = projectRows[index];

            List<string> managers = await dbContext.ContractManagers
                .AsNoTracking()
                .Where(manager => manager.ContractId == row.ContractId)
                .OrderBy(manager => manager.Employee.FullName)
                .Select(manager => manager.Employee.FullName)
                .ToListAsync(cancellationToken);

            string projectStatus = TimesheetWorkflow.ResolveProjectDisplayStatus(row.TimesheetStatusId, attendanceInfo.Status);

            items.Add(new OverviewItem(
                row.TimesheetId,
                "project",
                $"Projektová činnost {index + 1}",
                row.ContractName,
                row.Position,
                row.Workload,
                managers,
                projectStatus,
                row.ContractId,
                row.ProjectId
            ));
        }

        return TypedResults.Ok(new Response(request.EmployeeId, request.Year, request.Month, attendanceInfo.Status, items));
    }

}
