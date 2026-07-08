using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Projects.Endpoints;

public sealed class GetProject : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}", Handle)
           .WithSummary("Get Project");

    public sealed record ProjectItem(Guid Id, string Name, string RegistrationNumber);
    public sealed record Response(ProjectItem Project);

    private static async Task<Results<Ok<Response>, NotFound, ForbidHttpResult>> Handle(Guid id, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        ProjectItem? project = await dbContext.Projects
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProjectItem(
                p.Id,
                p.Name,
                p.RegistrationNumber
            ))
            .SingleOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            return TypedResults.NotFound();
        }

        if (!user.Satisfies(UserRole.Employee, projectId: id))
        {
            return TypedResults.Forbid();
        }

        return TypedResults.Ok(new Response(project));
    }
}
