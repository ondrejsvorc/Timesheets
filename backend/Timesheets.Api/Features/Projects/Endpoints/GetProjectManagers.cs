using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Projects.Endpoints;

public sealed class GetProjectManagers : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}/managers", Handle)
           .WithSummary("Get Project Managers");

    public sealed record ProjectManagerItem(
        Guid ProjectId,
        Guid EmployeeId,
        string EmployeePersonalNumber,
        string EmployeeFullName);
    public sealed record Response(IEnumerable<ProjectManagerItem> Managers);

    private static async Task<Results<Ok<Response>, ForbidHttpResult>> Handle(Guid id, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.Satisfies(UserRole.ProjectManager, projectId: id))
        {
            return TypedResults.Forbid();
        }

        List<ProjectManagerItem> managers = (await dbContext.ProjectManagers
            .AsNoTracking()
            .Where(pm => pm.ProjectId == id)
            .Include(pm => pm.Employee)
            .OrderBy(pm => pm.Employee.Surname)
            .ThenBy(pm => pm.Employee.FirstName)
            .ToListAsync(cancellationToken))
            .Select(pm => new ProjectManagerItem(
                pm.ProjectId,
                pm.EmployeeId,
                pm.Employee.PersonalNumber,
                pm.Employee.DisplayName
            ))
            .ToList();

        return TypedResults.Ok(new Response(managers));
    }
}
