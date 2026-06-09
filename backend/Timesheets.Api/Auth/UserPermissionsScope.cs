using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Auth;

public sealed record UserPermissionsScope(
    Guid EmployeeId,
    bool IsRoleManager,
    bool IsGlobalManager,
    bool HasGlobalScope,
    IReadOnlyList<Guid> ProjectManagerOf,
    IReadOnlyList<Guid> ContractManagerOf,
    IReadOnlyList<Guid> EmployeeOnContractIds,
    IReadOnlyList<Guid> VisibleProjectIds,
    IReadOnlyList<Guid> VisibleContractIds)
{
    public bool CanListEmployees =>
        HasGlobalScope || ProjectManagerOf.Count > 0 || ContractManagerOf.Count > 0;

    public bool CanViewAllProjects => HasGlobalScope;
}

internal static class UserPermissionsScopeLoader
{
    public static async Task<UserPermissionsScope?> LoadAsync(
        Employee employee,
        AppDbContext dbContext,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        bool isRoleManager = RoleManagerAuthorization.IsRoleManager(employee.Email, administrationOptions.Value);
        bool hasGlobalScope = employee.IsGlobalManager || isRoleManager;

        List<Guid> projectManagerOf = await dbContext.ProjectManagers
            .AsNoTracking()
            .Where(pm => pm.EmployeeId == employee.Id)
            .Select(pm => pm.ProjectId)
            .ToListAsync(cancellationToken);

        List<Guid> contractManagerOf = await dbContext.ContractManagers
            .AsNoTracking()
            .Where(cm => cm.EmployeeId == employee.Id)
            .Select(cm => cm.ContractId)
            .ToListAsync(cancellationToken);

        List<Guid> employeeOnContractIds = await dbContext.ContractEmployees
            .AsNoTracking()
            .Where(ce => ce.EmployeeId == employee.Id)
            .Select(ce => ce.ContractId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (hasGlobalScope)
        {
            List<Guid> allProjectIds = await dbContext.Projects
                .AsNoTracking()
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            List<Guid> allContractIds = await dbContext.Contracts
                .AsNoTracking()
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            return new UserPermissionsScope(
                employee.Id,
                isRoleManager,
                employee.IsGlobalManager,
                HasGlobalScope: true,
                projectManagerOf,
                contractManagerOf,
                employeeOnContractIds,
                allProjectIds,
                allContractIds);
        }

        HashSet<Guid> visibleContractIds = contractManagerOf
            .Concat(employeeOnContractIds)
            .ToHashSet();

        List<Guid> managedContractProjectIds = contractManagerOf.Count == 0
            ? []
            : await dbContext.Contracts
                .AsNoTracking()
                .Where(c => contractManagerOf.Contains(c.Id))
                .Select(c => c.ProjectId)
                .Distinct()
                .ToListAsync(cancellationToken);

        List<Guid> employeeContractProjectIds = employeeOnContractIds.Count == 0
            ? []
            : await dbContext.Contracts
                .AsNoTracking()
                .Where(c => employeeOnContractIds.Contains(c.Id))
                .Select(c => c.ProjectId)
                .Distinct()
                .ToListAsync(cancellationToken);

        HashSet<Guid> visibleProjectIds = projectManagerOf
            .Concat(managedContractProjectIds)
            .Concat(employeeContractProjectIds)
            .ToHashSet();

        List<Guid> projectContractIds = visibleProjectIds.Count == 0
            ? []
            : await dbContext.Contracts
                .AsNoTracking()
                .Where(c => visibleProjectIds.Contains(c.ProjectId))
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

        foreach (Guid contractId in projectContractIds)
        {
            visibleContractIds.Add(contractId);
        }

        return new UserPermissionsScope(
            employee.Id,
            isRoleManager,
            employee.IsGlobalManager,
            HasGlobalScope: false,
            projectManagerOf,
            contractManagerOf,
            employeeOnContractIds,
            visibleProjectIds.ToList(),
            visibleContractIds.ToList());
    }
}
