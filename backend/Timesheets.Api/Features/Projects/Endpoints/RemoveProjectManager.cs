using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Projects.Endpoints;

public sealed class RemoveProjectManager : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/{id}/managers/{employeeId}", Handle)
           .WithSummary("Remove Manager from Project");

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> Handle(Guid id, Guid employeeId, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.Satisfies(UserRole.ProjectManager, projectId: id))
        {
            return TypedResults.Forbid();
        }

        int affected = await dbContext.ProjectManagers
            .Where(pm => pm.ProjectId == id && pm.EmployeeId == employeeId)
            .ExecuteDeleteAsync(cancellationToken);

        if (affected == 0)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }
}
