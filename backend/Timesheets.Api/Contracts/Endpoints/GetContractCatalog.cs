using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Contracts.Endpoints;

public sealed class GetContractCatalog : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/catalog", Handle)
           .WithSummary("Get Contract Catalog");

    public sealed record ContractItem(Guid Id, string Name);
    public sealed record Response(IEnumerable<ContractItem> Contracts);

    private static async Task<Ok<Response>> Handle(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        List<ContractItem> contracts = await dbContext.Contracts
            .AsNoTracking()
            .Select(c => new ContractItem(c.Id, c.Name))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(contracts));
    }
}
