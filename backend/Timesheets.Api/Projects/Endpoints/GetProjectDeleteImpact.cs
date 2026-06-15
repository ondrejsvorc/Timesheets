using Microsoft.AspNetCore.Http.HttpResults;
using Timesheets.Api.Auth;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class GetProjectDeleteImpact : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}/delete-impact", Handle)
           .WithSummary("Get Project Delete Impact");

    private static async Task<Results<Ok<ProjectDeleteImpact>, NotFound, ForbidHttpResult>> Handle(Guid id, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.IsGlobalManagerRole())
        {
            return TypedResults.Forbid();
        }

        ProjectDeleteImpact? impact = await ProjectDeleteImpactCalculator.ForProjectAsync(
            id,
            dbContext,
            cancellationToken);

        return impact is null ? TypedResults.NotFound() : TypedResults.Ok(impact);
    }
}
