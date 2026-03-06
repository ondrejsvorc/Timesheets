using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Employees.Endpoints;

public sealed class GetEmployeeTimesheets : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}/timesheets", Handle)
           .WithSummary("Get Employee Timesheets");

    public sealed record Request([FromQuery] int? Year, [FromQuery] string? Months);
    public sealed record EmployeeTimesheetItem(Guid Id, Guid ContractId, string ContractName, int Year, int Month, Guid StatusId, string Status);
    public sealed record Response(Guid EmployeeId, IEnumerable<EmployeeTimesheetItem> Timesheets);

    private static async Task<Results<Ok<Response>, NotFound>> Handle(Guid id, [AsParameters] Request request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        bool employeeExists = await dbContext.Employees
            .AsNoTracking()
            .AnyAsync(e => e.Id == id, cancellationToken);

        if (!employeeExists)
        {
            return TypedResults.NotFound();
        }

        IQueryable<Data.Models.AttendanceTimesheet> query = dbContext.AttendanceTimesheets
            .AsNoTracking()
            .Where(timesheet => timesheet.EmployeeId == id);

        if (request.Year.HasValue)
        {
            query = query.Where(timesheet => timesheet.Year == request.Year.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Months))
        {
            List<int> validMonths = request.Months
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out int m) ? m : 0)
                .Where(m => m >= 1 && m <= 12)
                .ToList();
            if (validMonths.Count > 0)
            {
                query = query.Where(timesheet => validMonths.Contains(timesheet.Month));
            }
        }

        List<EmployeeTimesheetItem> timesheets = await query
            .Select(timesheet => new EmployeeTimesheetItem(
                timesheet.Id,
                timesheet.ContractId,
                timesheet.Contract.Name,
                timesheet.Year,
                timesheet.Month,
                timesheet.TimesheetStatusId,
                timesheet.TimesheetStatus.Name
            ))
            .OrderBy(timesheet => timesheet.Year)
            .ThenBy(timesheet => timesheet.Month)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(id, timesheets));
    }
}