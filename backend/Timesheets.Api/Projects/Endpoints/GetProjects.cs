using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Auth;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class GetProjects : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/", Handle)
           .WithSummary("Get Projects");

    public sealed record Response(IEnumerable<ProjectItem> Projects);

    private static async Task<Results<Ok<Response>, UnauthorizedHttpResult>> Handle(AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        IQueryable<Data.Models.Project> query = dbContext.Projects.AsNoTracking();

        if (!user.IsGlobalManagerRole())
        {
            if (user.VisibleProjectIds.Count == 0)
            {
                return TypedResults.Ok(new Response([]));
            }

            query = query.Where(p => user.VisibleProjectIds.Contains(p.Id));
        }

        List<ProjectItem> projects = await query
            .Select(p => new ProjectItem(
                p.Id,
                p.Name,
                p.RegistrationNumber,
                p.StartDate,
                p.EndDate,
                p.ArchivedAt,
                p.Contracts.Count
            ))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(projects));
    }
}
