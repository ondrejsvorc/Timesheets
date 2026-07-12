using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common;
using Timesheets.Api.Domain;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Projects.Endpoints;

public sealed class GetProject : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}", Handle)
           .WithSummary("Get Project");

    public sealed record ProjectItem(Guid Id, string Name, string RegistrationNumber, DateTime? ArchivedAt, string Status);
    public sealed record Response(ProjectItem Project);

    private static async Task<Results<Ok<Response>, NotFound, ForbidHttpResult>> Handle(Guid id, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        Domain.Models.Project? project = await dbContext.Projects
            .AsNoTracking()
            .Where(p => p.Id == id)
            .SingleOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            return TypedResults.NotFound();
        }

        if (!user.Satisfies(UserRole.Employee, projectId: id))
        {
            return TypedResults.Forbid();
        }

        ProjectItem item = new(project.Id, project.Name, project.RegistrationNumber, project.ArchivedAt, project.GetStatus(PragueClock.Today));

        return TypedResults.Ok(new Response(item));
    }
}
