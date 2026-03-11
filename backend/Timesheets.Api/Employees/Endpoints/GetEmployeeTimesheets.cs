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
    public sealed record AvailableMonthItem(int Year, int Month, bool HasUnapproved);
    private sealed record AvailableMonthSourceItem(int Year, int Month, string Status);
    public sealed record Response(Guid EmployeeId, IEnumerable<EmployeeTimesheetItem> Timesheets, IEnumerable<int> AvailableYears, IEnumerable<AvailableMonthItem> AvailableMonths);

    private static async Task<Results<Ok<Response>, NotFound>> Handle(Guid id, [AsParameters] Request request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        bool employeeExists = await dbContext.Employees
            .AsNoTracking()
            .AnyAsync(e => e.Id == id, cancellationToken);

        if (!employeeExists)
        {
            return TypedResults.NotFound();
        }

        IQueryable<Data.Models.AttendanceTimesheet> baseQuery = dbContext.AttendanceTimesheets
            .AsNoTracking()
            .Where(timesheet => timesheet.EmployeeId == id);

        List<AvailableMonthSourceItem> monthRows = await baseQuery
            .Select(timesheet => new AvailableMonthSourceItem(timesheet.Year, timesheet.Month, timesheet.TimesheetStatus.Name))
            .ToListAsync(cancellationToken);

        List<AvailableMonthItem> availableMonths = monthRows
            .GroupBy(item => new { item.Year, item.Month })
            .OrderBy(group => group.Key.Year)
            .ThenBy(group => group.Key.Month)
            .Select(group => new AvailableMonthItem(
                group.Key.Year,
                group.Key.Month,
                group.Any(item => item.Status != "Schválený")
            ))
            .ToList();

        List<int> availableYears = availableMonths
            .Select(item => item.Year)
            .Distinct()
            .OrderBy(year => year)
            .ToList();

        IQueryable<Data.Models.AttendanceTimesheet> query = baseQuery;

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
            .OrderBy(timesheet => timesheet.Year)
            .ThenBy(timesheet => timesheet.Month)
            .Select(timesheet => new EmployeeTimesheetItem(
                timesheet.Id,
                timesheet.ContractId,
                timesheet.Contract.Name,
                timesheet.Year,
                timesheet.Month,
                timesheet.TimesheetStatusId,
                timesheet.TimesheetStatus.Name
            ))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(id, timesheets, availableYears, availableMonths));
    }
}