using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class GetProjects : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/", Handle)
           .WithSummary("Get Projects");

    public sealed record Response(IEnumerable<ProjectItem> Projects);

    private static async Task<Ok<Response>> Handle(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        List<ProjectItem> projects = await dbContext.Projects
            .AsNoTracking()
            .Select(p => new ProjectItem(
                p.Id,
                p.Name,
                p.RegistrationNumber,
                p.StartDate,
                p.EndDate,
                p.Contracts.Count
            ))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(projects));
    }
}

