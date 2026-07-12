using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Projects.Endpoints;

public sealed class DeleteProject : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/{id}", Handle)
           .WithSummary("Delete Project");

    private static async Task<Results<NoContent, NotFound, Conflict<string>, ForbidHttpResult>> Handle(Guid id, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.IsGlobalManagerRole())
        {
            return TypedResults.Forbid();
        }

        bool exists = await dbContext.Projects.AsNoTracking().AnyAsync(p => p.Id == id, cancellationToken);
        if (!exists)
        {
            return TypedResults.NotFound();
        }

        List<Guid> contractIds = await dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.ProjectId == id)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        if (await DeleteImpactCore.HasProtectedTimesheetsAsync(contractIds, dbContext, cancellationToken))
        {
            return TypedResults.Conflict("Projekt nelze smazat, protože obsahuje výkazy ke schválení nebo schválené.");
        }

        await dbContext.Projects
            .Where(p => p.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
