using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
using Timesheets.Api.Auth;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class DeleteProject : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/{id}", Handle)
           .WithSummary("Delete Project");

    private static async Task<Results<NoContent, NotFound, Conflict<string>, ForbidHttpResult>> Handle(
        Guid id,
        [FromQuery] bool force,
        HttpContext httpContext,
        AppDbContext dbContext,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        (_, UserPermissionsScope scope) = await PermissionsScopeResolver.ResolveRequiredAsync(
            httpContext, dbContext, administrationOptions, cancellationToken);

        if (!ApiPermissions.CanModifyProjects(scope))
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

        if (await ProjectDeleteImpactCalculator.HasProtectedTimesheetsAsync(contractIds, dbContext, cancellationToken))
        {
            if (!force)
            {
                return TypedResults.Conflict("Projekt obsahuje odeslané nebo schválené projektové výkazy a nelze ho smazat.");
            }

            if (!scope.HasGlobalScope)
            {
                return TypedResults.Forbid();
            }
        }

        await dbContext.Projects
            .Where(p => p.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
