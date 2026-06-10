using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
using Timesheets.Api.Auth;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class UnarchiveProject : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/{id}/unarchive", Handle)
           .WithSummary("Unarchive Project")
           .DisableAntiforgery();

    public sealed record Response(ProjectItem Project);

    private static async Task<Results<Ok<Response>, NotFound, BadRequest<string>, ForbidHttpResult>> Handle(
        Guid id,
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

        Data.Models.Project? project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (project is null)
        {
            return TypedResults.NotFound();
        }

        if (!project.ArchivedAt.HasValue)
        {
            return TypedResults.BadRequest("Projekt není archivován.");
        }

        project.ArchivedAt = null;
        project.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        int contractCount = await dbContext.Contracts.CountAsync(c => c.ProjectId == id, cancellationToken);

        return TypedResults.Ok(new Response(new ProjectItem(
            project.Id,
            project.Name,
            project.RegistrationNumber,
            project.StartDate,
            project.EndDate,
            project.ArchivedAt,
            contractCount)));
    }
}
