using Microsoft.AspNetCore.Http.HttpResults;
using Timesheets.Api.Auth;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class GetProjectContractDeleteImpact : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{projectId}/contracts/{contractId}/delete-impact", Handle)
           .WithSummary("Get Project Contract Delete Impact");

    private static async Task<Results<Ok<ProjectDeleteImpact>, NotFound, ForbidHttpResult>> Handle(Guid projectId, Guid contractId, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.IsGlobalManagerRole())
        {
            return TypedResults.Forbid();
        }

        ProjectDeleteImpact? impact = await ProjectDeleteImpactCalculator.ForContractAsync(
            projectId,
            contractId,
            user.IsGlobalManagerRole(),
            dbContext,
            cancellationToken);

        return impact is null ? TypedResults.NotFound() : TypedResults.Ok(impact);
    }
}
