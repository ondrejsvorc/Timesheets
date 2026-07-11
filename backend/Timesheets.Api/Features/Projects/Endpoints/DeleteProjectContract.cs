using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Projects.Endpoints;

public sealed class DeleteProjectContract : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/{contractEmployeeId}/contracts/{contractId}", Handle)
           .WithSummary("Delete Project Contract");

    private static async Task<Results<NoContent, NotFound, Conflict<string>, ForbidHttpResult>> Handle(Guid contractEmployeeId, Guid contractId, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.CanManageContract(contractId, contractEmployeeId))
        {
            return TypedResults.Forbid();
        }

        bool exists = await dbContext.Contracts
            .AsNoTracking()
            .AnyAsync(c => c.ProjectId == contractEmployeeId && c.Id == contractId, cancellationToken);

        if (!exists)
        {
            return TypedResults.NotFound();
        }

        if (await DeleteImpactCore.HasProtectedTimesheetsAsync([contractId], dbContext, cancellationToken))
        {
            return TypedResults.Conflict("Zakázku nelze smazat, protože obsahuje výkazy ke schválení nebo schválené.");
        }

        await dbContext.Contracts
            .Where(c => c.ProjectId == contractEmployeeId && c.Id == contractId)
            .ExecuteDeleteAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
