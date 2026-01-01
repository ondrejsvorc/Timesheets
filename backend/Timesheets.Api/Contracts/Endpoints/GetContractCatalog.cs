using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Contracts.Endpoints;

public sealed class GetContractCatalog : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/catalog", Handle)
           .WithSummary("Get Contract Catalog");

    public sealed record ContractItem(Guid Id, Guid ProjectId, string Name);
    public sealed record Response(IEnumerable<ContractItem> Contracts);

    private static async Task<Ok<Response>> Handle(Guid? projectId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        IQueryable<Contract> query = dbContext.Contracts.AsNoTracking();

        if (projectId.HasValue)
        {
            query = query.Where(c => c.ProjectId == projectId.Value);
        }

        List<ContractItem> contracts = await query
            .AsNoTracking()
            .Select(c => new ContractItem(c.Id, c.ProjectId, c.Name))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(contracts));
    }
}
