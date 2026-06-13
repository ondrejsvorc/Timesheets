using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Auth;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class DeleteProjectContract : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/{projectId}/contracts/{contractId}", Handle)
           .WithSummary("Delete Project Contract");

    private static async Task<Results<NoContent, NotFound, Conflict<string>, ForbidHttpResult>> Handle(Guid projectId, Guid contractId, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken, [FromQuery] bool force = false)
    {
        if (!user.IsGlobalManagerRole())
        {
            return TypedResults.Forbid();
        }

        bool exists = await dbContext.Contracts
            .AsNoTracking()
            .AnyAsync(c => c.ProjectId == projectId && c.Id == contractId, cancellationToken);

        if (!exists)
        {
            return TypedResults.NotFound();
        }

        if (await ProjectDeleteImpactCalculator.HasProtectedTimesheetsAsync([contractId], dbContext, cancellationToken))
        {
            if (!force)
            {
                return TypedResults.Conflict("Zakázku nelze smazat — jsou zde výkazy ke schválení nebo schválené.");
            }

            if (!user.IsGlobalManagerRole())
            {
                return TypedResults.Forbid();
            }
        }

        await dbContext.Contracts
            .Where(c => c.ProjectId == projectId && c.Id == contractId)
            .ExecuteDeleteAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
