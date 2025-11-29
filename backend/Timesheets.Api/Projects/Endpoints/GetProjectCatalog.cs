using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class GetProjectCatalog : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/catalog", Handle)
           .WithSummary("Get Project Catalog");

    public sealed record ProjectItem(Guid Id, string Name);
    public sealed record Response(IEnumerable<ProjectItem> Projects);

    private static async Task<Ok<Response>> Handle(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        List<ProjectItem> projects = await dbContext.Projects
            .AsNoTracking()
            .Select(p => new ProjectItem(p.Id, p.Name))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(projects));
    }
}
