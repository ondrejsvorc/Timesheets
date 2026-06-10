using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
using Timesheets.Api.Auth;
using Timesheets.Api.Common;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class GetProjects : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/", Handle)
           .WithSummary("Get Projects");

    public sealed record Response(IEnumerable<ProjectItem> Projects);

    private static async Task<Results<Ok<Response>, UnauthorizedHttpResult>> Handle(
        HttpContext httpContext,
        AppDbContext dbContext,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        Employee employee = await CurrentEmployeeResolver.GetRequiredAsync(httpContext.User, dbContext, cancellationToken);
        UserPermissionsScope scope = await UserPermissionsScopeLoader.LoadAsync(employee, dbContext, administrationOptions, cancellationToken)
            ?? throw new InvalidOperationException("Employee permissions scope was not found.");

        IQueryable<Data.Models.Project> query = dbContext.Projects.AsNoTracking();

        if (!scope.CanViewAllProjects)
        {
            if (scope.VisibleProjectIds.Count == 0)
            {
                return TypedResults.Ok(new Response([]));
            }

            query = query.Where(p => scope.VisibleProjectIds.Contains(p.Id));
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

