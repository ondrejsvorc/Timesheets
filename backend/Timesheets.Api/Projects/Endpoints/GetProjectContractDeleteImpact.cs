using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
using Timesheets.Api.Auth;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class GetProjectContractDeleteImpact : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{projectId}/contracts/{contractId}/delete-impact", Handle)
           .WithSummary("Get Project Contract Delete Impact");

    private static async Task<Results<Ok<ProjectDeleteImpact>, NotFound, ForbidHttpResult>> Handle(
        Guid projectId,
        Guid contractId,
        HttpContext httpContext,
        AppDbContext dbContext,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        (_, UserPermissionsScope scope) = await PermissionsScopeResolver.ResolveRequiredAsync(
            httpContext, dbContext, administrationOptions, cancellationToken);

        if (!ApiPermissions.CanModifyProjects(scope))
        {
            return TypedResults.Forbid();
        }

        ProjectDeleteImpact? impact = await ProjectDeleteImpactCalculator.ForContractAsync(
            projectId,
            contractId,
            scope.HasGlobalScope,
            dbContext,
            cancellationToken);

        return impact is null ? TypedResults.NotFound() : TypedResults.Ok(impact);
    }
}
