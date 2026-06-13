using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Auth;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class GetProjectCatalog : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/catalog", Handle)
           .WithSummary("Get Project Catalog");

    public sealed record ProjectItem(Guid Id, string Name);
    public sealed record Response(IEnumerable<ProjectItem> Projects);

    private static async Task<Results<Ok<Response>, ForbidHttpResult>> Handle(AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.IsContractManager())
        {
            return TypedResults.Forbid();
        }

        IQueryable<Project> query = dbContext.Projects.AsNoTracking();

        if (!user.IsGlobalManagerRole())
        {
            query = query.Where(p => user.VisibleProjectIds.Contains(p.Id));
        }

        List<ProjectItem> projects = await query
            .Select(p => new ProjectItem(p.Id, p.Name))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(projects));
    }
}
