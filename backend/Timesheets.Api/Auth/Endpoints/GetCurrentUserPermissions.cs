using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Auth.Endpoints;

public sealed class GetCurrentUserPermissions : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/currentUserPermissions", Handle)
           .WithSummary("Get Currently Authenticated User Permissions");

    public sealed record Response(
        bool IsRoleManager,
        bool IsGlobalManager,
        IReadOnlyList<Guid> ProjectManagerOf,
        IReadOnlyList<Guid> ContractManagerOf,
        IReadOnlyList<Guid> EmployeeOnContractIds,
        IReadOnlyList<Guid> VisibleProjectIds,
        IReadOnlyList<Guid> VisibleContractIds);

    private static async Task<IResult> Handle(
        HttpContext httpContext,
        AppDbContext dbContext,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        ClaimsPrincipal currentUser = httpContext.User;
        if (!currentUser.IsAuthenticated())
        {
            return Results.Unauthorized();
        }

        string email = currentUser.GetEmail();

        Employee? employee = await dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Email == email)
            .FirstOrDefaultAsync(cancellationToken);

        if (employee is null)
        {
            return Results.NotFound("Employee not found.");
        }

        UserPermissionsScope? scope = await UserPermissionsScopeLoader.LoadAsync(
            employee,
            dbContext,
            administrationOptions,
            cancellationToken);

        if (scope is null)
        {
            return Results.NotFound("Employee not found.");
        }

        Response response = new(
            IsRoleManager: scope.IsRoleManager,
            IsGlobalManager: scope.IsGlobalManager,
            ProjectManagerOf: scope.ProjectManagerOf,
            ContractManagerOf: scope.ContractManagerOf,
            EmployeeOnContractIds: scope.EmployeeOnContractIds,
            VisibleProjectIds: scope.VisibleProjectIds,
            VisibleContractIds: scope.VisibleContractIds);

        return Results.Ok(response);
    }
}
