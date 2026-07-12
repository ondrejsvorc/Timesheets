using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common;
using Timesheets.Api.Domain;
using Timesheets.Api.Domain.Models;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Projects.Endpoints;

public sealed class GetProjects : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/", Handle)
           .WithSummary("Get Projects");

    public sealed record Response(IEnumerable<ProjectItem> Projects);

    private static async Task<Results<Ok<Response>, UnauthorizedHttpResult>> Handle(AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        IQueryable<Project> query = dbContext.Projects.AsNoTracking();

        if (!user.IsGlobalManagerRole())
        {
            if (user.VisibleProjectIds.Count == 0)
            {
                return TypedResults.Ok(new Response([]));
            }

            query = query.Where(p => user.VisibleProjectIds.Contains(p.Id));
        }

        DateOnly today = PragueClock.Today;
        List<ProjectItem> projects = (await query
            .Select(p => new
            {
                Project = p,
                ContractCount = p.Contracts.Count
            })
            .ToListAsync(cancellationToken))
            .Select(p => new ProjectItem(p.Project.Id, p.Project.Name, p.Project.RegistrationNumber, p.Project.StartDate, p.Project.EndDate, p.Project.ArchivedAt, p.ContractCount, p.Project.GetStatus(today)))
            .ToList();

        return TypedResults.Ok(new Response(projects));
    }
}
