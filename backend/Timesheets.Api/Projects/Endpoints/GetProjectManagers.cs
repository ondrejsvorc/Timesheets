using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Auth;
using Timesheets.Api.Common;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects.Endpoints;

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

        List<ProjectManagerItem> managers = await dbContext.ProjectManagers
            .AsNoTracking()
            .Where(pm => pm.ProjectId == id)
            .OrderBy(pm => pm.Employee.FullName)
            .Select(pm => new ProjectManagerItem(
                pm.ProjectId,
                pm.EmployeeId,
                pm.Employee.PersonalNumber,
                EmployeeNameFormatter.Format(pm.Employee.TitleBefore, pm.Employee.FullName, pm.Employee.TitleAfter)
            ))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(managers));
    }
}
