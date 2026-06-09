using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Auth;

public static class ApiPermissions
{
    public static bool CanAccessProject(UserPermissionsScope scope, Guid projectId) =>
        scope.HasGlobalScope || scope.VisibleProjectIds.Contains(projectId);

    public static bool CanAccessContract(UserPermissionsScope scope, Guid contractId) =>
        scope.HasGlobalScope || scope.VisibleContractIds.Contains(contractId);

    public static bool CanModifyProjects(UserPermissionsScope scope) => scope.HasGlobalScope;

    public static bool CanManageContractEmployees(UserPermissionsScope scope, Guid contractId) =>
        scope.HasGlobalScope || scope.ContractManagerOf.Contains(contractId);

    public static bool CanManageContractManagers(UserPermissionsScope scope, Guid projectId) =>
        scope.HasGlobalScope || scope.ProjectManagerOf.Contains(projectId);

    public static bool CanImportTimesheets(UserPermissionsScope scope) => scope.HasGlobalScope;

    public static bool CanEditEmployeeType(UserPermissionsScope scope) => scope.HasGlobalScope;

    public static bool CanManageEmployeePositions(UserPermissionsScope scope) => scope.CanListEmployees;

    public static bool CanUpdateOwnTimesheet(UserPermissionsScope scope, Guid timesheetOwnerId) =>
        scope.EmployeeId == timesheetOwnerId;

    public static async Task<bool> CanAccessEmployeeAsync(
        UserPermissionsScope scope,
        Guid employeeId,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (scope.EmployeeId == employeeId)
        {
            return true;
        }

        if (!scope.CanListEmployees)
        {
            return false;
        }

        if (scope.HasGlobalScope)
        {
            return await dbContext.Employees.AsNoTracking().AnyAsync(e => e.Id == employeeId, cancellationToken);
        }

        HashSet<Guid> visibleContractIds = scope.VisibleContractIds.ToHashSet();
        HashSet<Guid> visibleProjectIds = scope.VisibleProjectIds.ToHashSet();

        return await dbContext.Employees.AsNoTracking().AnyAsync(
            e => e.Id == employeeId
                && dbContext.ContractEmployees.Any(ce =>
                    ce.EmployeeId == e.Id
                    && (visibleContractIds.Contains(ce.ContractId)
                        || dbContext.Contracts.Any(c => c.Id == ce.ContractId && visibleProjectIds.Contains(c.ProjectId)))),
            cancellationToken);
    }

    public static async Task<Guid?> GetProjectIdForContractAsync(
        Guid contractId,
        AppDbContext dbContext,
        CancellationToken cancellationToken) =>
        await dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.Id == contractId)
            .Select(c => (Guid?)c.ProjectId)
            .FirstOrDefaultAsync(cancellationToken);

    public static async Task<bool> CanManageContractManagersForContractAsync(
        UserPermissionsScope scope,
        Guid contractId,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        Guid? projectId = await GetProjectIdForContractAsync(contractId, dbContext, cancellationToken);
        return projectId.HasValue && CanManageContractManagers(scope, projectId.Value);
    }
}
