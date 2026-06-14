using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
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
        Guid? projectId = await dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => (Guid?)c.ProjectId)
            .FirstOrDefaultAsync(cancellationToken);

        if (projectId is null)
        {
            return TypedResults.NotFound();
        }

        if (!user.CanManageContract(id, projectId.Value))
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
