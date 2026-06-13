using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class GetTimesheetCatalog : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/catalog", Handle)
           .WithSummary("Get Timesheet Catalog");

    public sealed record Request([FromQuery] Guid EmployeeId, [FromQuery] int Year, [FromQuery] int Month);
    public sealed record ProjectTimesheetItem(Guid Id, string Label);
    public sealed record Response(Guid AttendanceTimesheetId, Guid CurrentStatusId, IEnumerable<ProjectTimesheetItem> ProjectTimesheets);
    private sealed record ProjectTimesheetRow(Guid Id, string ContractName);

    private static async Task<Results<Ok<Response>, NotFound>> Handle([AsParameters] Request request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var attendanceTimesheet = await dbContext.AttendanceTimesheets
            .AsNoTracking()
            .Where(t => t.EmployeeId == request.EmployeeId && t.Year == request.Year && t.Month == request.Month)
            .Select(t => new { t.Id, t.TimesheetStatusId })
            .SingleOrDefaultAsync(cancellationToken);

        if (attendanceTimesheet is null)
        {
            return TypedResults.NotFound();
        }

        List<ProjectTimesheetRow> projectRows = await dbContext.ProjectTimesheets
            .AsNoTracking()
            .Where(timesheet => timesheet.EmployeeId == request.EmployeeId && timesheet.Year == request.Year && timesheet.Month == request.Month)
            .Join(dbContext.ContractEmployees.AsNoTracking(), timesheet => timesheet.ContractEmployeeId, contractEmployee => contractEmployee.Id, (timesheet, contractEmployee) => new { timesheet, contractEmployee })
            .Join(dbContext.Contracts.AsNoTracking(), x => x.contractEmployee.ContractId, contract => contract.Id, (x, contract) => new { x.timesheet, contract })
            .OrderBy(x => x.contract.Name)
            .Select(x => new ProjectTimesheetRow(x.timesheet.Id, x.contract.Name))
            .ToListAsync(cancellationToken);

        List<ProjectTimesheetItem> projectTimesheets = projectRows
            .Select((row, index) => new ProjectTimesheetItem(row.Id, $"Projektová činnost {index + 1}"))
            .ToList();

        return TypedResults.Ok(new Response(attendanceTimesheet.Id, attendanceTimesheet.TimesheetStatusId, projectTimesheets));
    }
}
