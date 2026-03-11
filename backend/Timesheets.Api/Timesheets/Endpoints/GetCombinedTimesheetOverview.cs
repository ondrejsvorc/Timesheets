using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class GetCombinedTimesheetOverview : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/combined/overview", Handle)
           .WithSummary("Get Combined Timesheet Overview");

    public sealed record Request([FromQuery] Guid EmployeeId, [FromQuery] int Year, [FromQuery] int Month);
    public sealed record OverviewItem(string Label, string? ContractName, string? Position, decimal Workload, IEnumerable<string> Managers);
    public sealed record Response(Guid EmployeeId, int Year, int Month, IEnumerable<OverviewItem> Items);
    private sealed record ProjectRowSource(Guid ContractId, string ContractName, decimal Workload);

    private static async Task<Results<Ok<Response>, NotFound>> Handle([AsParameters] Request request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var attendanceInfo = await dbContext.AttendanceTimesheets
            .AsNoTracking()
            .Where(t => t.EmployeeId == request.EmployeeId && t.Year == request.Year && t.Month == request.Month)
            .Select(t => new
            {
                Days = t.Days.Select(d => d.Workload).ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (attendanceInfo is null)
        {
            return TypedResults.NotFound();
        }

        List<ProjectRowSource> projectRows = await dbContext.ProjectTimesheets
            .AsNoTracking()
            .Where(t => t.EmployeeId == request.EmployeeId && t.Year == request.Year && t.Month == request.Month)
            .Join(
                dbContext.Contracts.AsNoTracking(),
                timesheet => timesheet.ContractId,
                contract => contract.Id,
                (timesheet, contract) => new ProjectRowSource(contract.Id, contract.Name, timesheet.Workload)
            )
            .ToListAsync(cancellationToken);

        decimal totalProjectWorkload = projectRows.Sum(item => item.Workload);
        decimal totalWorkload = attendanceInfo.Days.FirstOrDefault(value => value.HasValue) ?? (totalProjectWorkload > 0 ? totalProjectWorkload : 1m);
        decimal coreWorkload = Math.Max(0m, totalWorkload - totalProjectWorkload);

        DateTime periodStart = new(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime periodEnd = periodStart.AddMonths(1).AddDays(-1);

        List<OverviewItem> items =
        [
            new("Kmenový úvazek", null, null, coreWorkload, []),
        ];

        for (int index = 0; index < projectRows.Count; index++)
        {
            ProjectRowSource row = projectRows[index];

            List<string> positions = await dbContext.ContractEmployees
                .AsNoTracking()
                .Where(employee =>
                    employee.EmployeeId == request.EmployeeId
                    && employee.ContractId == row.ContractId
                    && employee.StartDate <= periodEnd
                    && (employee.EndDate == null || employee.EndDate >= periodStart)
                )
                .OrderBy(employee => employee.StartDate)
                .Select(employee => employee.Position ?? string.Empty)
                .Where(position => position != string.Empty)
                .ToListAsync(cancellationToken);

            List<string> managers = await dbContext.ContractManagers
                .AsNoTracking()
                .Where(manager => manager.ContractId == row.ContractId)
                .OrderBy(manager => manager.Employee.FullName)
                .Select(manager => manager.Employee.FullName)
                .ToListAsync(cancellationToken);

            items.Add(new OverviewItem(
                $"Projektová činnost {index + 1}",
                row.ContractName,
                positions.Count > 0 ? string.Join(", ", positions) : null,
                row.Workload,
                managers
            ));
        }

        return TypedResults.Ok(new Response(request.EmployeeId, request.Year, request.Month, items));
    }
}
