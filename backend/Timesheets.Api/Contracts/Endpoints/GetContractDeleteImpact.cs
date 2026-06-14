using Microsoft.AspNetCore.Http.HttpResults;
using Timesheets.Api.Auth;
using Timesheets.Api.Data;

namespace Timesheets.Api.Contracts.Endpoints;

public sealed class GetContractDeleteImpact : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}/delete-impact", Handle)
           .WithSummary("Get Contract Delete Impact");

    private static async Task<Results<Ok<ContractDeleteImpact>, NotFound, ForbidHttpResult>> Handle(Guid id, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.IsGlobalManagerRole())
        {
            return TypedResults.Forbid();
        }

        ContractDeleteImpact? impact = await ContractDeleteImpactCalculator.ForContractAsync(
            id,
            user.IsGlobalManagerRole(),
            dbContext,
            cancellationToken);

        return impact is null ? TypedResults.NotFound() : TypedResults.Ok(impact);
    }
}
