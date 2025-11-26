using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class GetProjectContracts : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}/contracts", Handle)
           .WithSummary("Get Project Contracts");

    public sealed record ContractItem(Guid Id, string Name, string? RegistrationNumber, DateTime StartDate, DateTime? EndDate, int EmployeeCount);
    public sealed record Response(IEnumerable<ContractItem> Contracts);

    private static async Task<Results<Ok<Response>, NotFound>> Handle(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        bool exists = await dbContext.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == id, cancellationToken);

        if (!exists)
        {
            return TypedResults.NotFound();
        }

        List<ContractItem> contracts = await dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.ProjectId == id)
            .Select(c => new ContractItem(
                c.Id,
                c.Name,
                c.RegistrationNumber,
                c.StartDate,
                c.EndDate,
                c.Employees.Count
            ))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(contracts));
    }
}
