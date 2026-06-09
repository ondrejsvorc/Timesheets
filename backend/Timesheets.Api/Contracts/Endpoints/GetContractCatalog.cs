using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
using Timesheets.Api.Auth;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Contracts.Endpoints;

public sealed class GetContractCatalog : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/catalog", Handle)
           .WithSummary("Get Contract Catalog");

    public sealed record ContractItem(Guid Id, Guid ProjectId, string Name);
    public sealed record Response(IEnumerable<ContractItem> Contracts);

    private static async Task<Results<Ok<Response>, ForbidHttpResult>> Handle(
        Guid? projectId,
        HttpContext httpContext,
        AppDbContext dbContext,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        (_, UserPermissionsScope scope) = await PermissionsScopeResolver.ResolveRequiredAsync(
            httpContext, dbContext, administrationOptions, cancellationToken);

        if (!ApiPermissions.CanManageEmployeePositions(scope))
        {
            return TypedResults.Forbid();
        }

        IQueryable<Contract> query = dbContext.Contracts.AsNoTracking();

        if (!scope.HasGlobalScope)
        {
            query = query.Where(c => scope.VisibleContractIds.Contains(c.Id));
        }

        if (projectId.HasValue)
        {
            query = query.Where(c => c.ProjectId == projectId.Value);
        }

        List<ContractItem> contracts = await query
            .Select(c => new ContractItem(c.Id, c.ProjectId, c.Name))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(contracts));
    }
}
