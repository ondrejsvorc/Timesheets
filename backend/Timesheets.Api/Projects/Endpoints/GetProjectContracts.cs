using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class GetProjectContracts : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}/contracts", Handle)
           .WithSummary("Get Project Contracts");

    public sealed record Response(IEnumerable<ProjectContractItem> ProjectContracts);

    private static async Task<Ok<Response>> Handle(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        List<ProjectContractItem> contracts = await dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.ProjectId == id)
            .Select(c => new ProjectContractItem(
                c.Id,
                c.Name,
                c.RegistrationNumber,
                c.StartDate,
                c.EndDate,
                c.ContractEmployees.Count
            ))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(contracts));
    }
}
