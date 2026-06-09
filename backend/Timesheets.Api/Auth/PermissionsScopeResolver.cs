using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
using Timesheets.Api.Common;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Auth;

internal static class PermissionsScopeResolver
{
    public static async Task<(Employee Employee, UserPermissionsScope Scope)> ResolveRequiredAsync(
        HttpContext httpContext,
        AppDbContext dbContext,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        Employee employee = await CurrentEmployeeResolver.GetRequiredAsync(httpContext.User, dbContext, cancellationToken);
        UserPermissionsScope scope = await UserPermissionsScopeLoader.LoadAsync(employee, dbContext, administrationOptions, cancellationToken)
            ?? throw new InvalidOperationException("Employee permissions scope was not found.");

        return (employee, scope);
    }
}
