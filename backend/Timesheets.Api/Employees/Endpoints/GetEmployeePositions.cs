using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Employees.Endpoints;

public sealed class GetEmployeePositions : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}/positions", Handle)
           .WithSummary("Get Employee Positions");

    public sealed record EmployeePositionItem(Guid ProjectId, string ProjectName, Guid ContractId, string ContractName, string Position, DateTime StartDate, DateTime? EndDate);
    public sealed record Response(IEnumerable<EmployeePositionItem> Positions);

    private static async Task<Results<Ok<Response>, NotFound>> Handle(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        List<EmployeePositionItem> positions = await dbContext.ContractEmployees
            .AsNoTracking()
            .Where(e => e.EmployeeId == id)
            .Select(e => new EmployeePositionItem(
                e.Contract.Project.Id,
                e.Contract.Project.Name,
                e.Contract.Id,
                e.Contract.Name,
                e.Position ?? string.Empty,
                e.StartDate,
                e.EndDate
            ))
            .OrderBy(p => p.ProjectName)
            .ThenBy(p => p.ContractName)
            .ThenBy(p => p.StartDate)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(positions));
    }
}