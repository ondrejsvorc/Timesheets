using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
using Timesheets.Api.Auth;
using Timesheets.Api.Common;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class GetProjectContractsManagers : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}/contracts/managers", Handle)
           .WithSummary("Get Project Contracts Managers");

    public sealed record ContractManagerItem(
        Guid ContractId,
        Guid EmployeeId,
        string ContractRegistrationNumber,
        string EmployeePersonalNumber,
        string EmployeeFullName,
        string EmployeeEmail);
    public sealed record Response(IEnumerable<ContractManagerItem> Managers);

    private static async Task<Results<Ok<Response>, ForbidHttpResult>> Handle(
        Guid id,
        HttpContext httpContext,
        AppDbContext dbContext,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        (_, UserPermissionsScope scope) = await PermissionsScopeResolver.ResolveRequiredAsync(
            httpContext, dbContext, administrationOptions, cancellationToken);

        if (!ApiPermissions.CanManageContractManagers(scope, id))
        {
            return TypedResults.Forbid();
        }

        List<ContractManagerItem> managers = await dbContext.ContractManagers
            .AsNoTracking()
            .Where(cm => cm.Contract.ProjectId == id)
            .OrderBy(cm => cm.Contract.RegistrationNumber)
            .ThenBy(cm => cm.Employee.FullName)
            .Select(cm => new ContractManagerItem(
                cm.ContractId,
                cm.EmployeeId,
                cm.Contract.RegistrationNumber,
                cm.Employee.PersonalNumber,
                EmployeeNameFormatter.Format(cm.Employee.TitleBefore, cm.Employee.FullName, cm.Employee.TitleAfter),
                cm.Employee.Email
            ))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(managers));
    }
}
