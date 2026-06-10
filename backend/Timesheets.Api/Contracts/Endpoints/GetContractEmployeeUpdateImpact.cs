using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
using Timesheets.Api.Auth;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Contracts.Endpoints;

public sealed class GetContractEmployeeUpdateImpact : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/{id}/employees/{contractEmployeeId}/update-impact", Handle)
           .WithSummary("Get Contract Employee Update Impact")
           .DisableAntiforgery();

    private static async Task<Results<Ok<ContractEmployeeUpdateImpact>, NotFound, ForbidHttpResult>> Handle(
        Guid id,
        Guid contractEmployeeId,
        [FromBody] ContractEmployeeUpdateRequest request,
        HttpContext httpContext,
        AppDbContext dbContext,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        (_, UserPermissionsScope scope) = await PermissionsScopeResolver.ResolveRequiredAsync(
            httpContext, dbContext, administrationOptions, cancellationToken);

        if (!await ApiPermissions.CanManageContractEmployeesAsync(scope, id, dbContext, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        ContractEmployee? existing = await dbContext.ContractEmployees
            .AsNoTracking()
            .FirstOrDefaultAsync(ce => ce.ContractId == id && ce.Id == contractEmployeeId, cancellationToken);

        if (existing is null)
        {
            return TypedResults.NotFound();
        }

        ContractEmployeeUpdateImpact impact = await ContractEmployeeUpdatePlanner.PlanAsync(
            existing,
            request,
            dbContext,
            cancellationToken);

        return TypedResults.Ok(impact);
    }
}
