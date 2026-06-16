using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Auth;

internal sealed class UserPermissions
{
    public Guid EmployeeId { get; init; }
    public UserRole Role { get; init; }
    public IReadOnlyList<Guid> ProjectManagerOf { get; init; } = [];
    public IReadOnlyList<Guid> ContractManagerOf { get; init; } = [];
    public IReadOnlyList<Guid> EmployeeOnContractIds { get; init; } = [];
    public IReadOnlyList<Guid> VisibleProjectIds { get; init; } = [];
    public IReadOnlyList<Guid> VisibleContractIds { get; init; } = [];
}

internal static class UserPermissionsLoader
{
    public static async Task<UserPermissions> LoadAsync(Employee employee, AppDbContext dbContext, IOptions<AdministrationOptions> administrationOptions, CancellationToken cancellationToken)
    {
        bool isRoleManager = RoleManagerAuthorization.IsRoleManager(employee.PersonalNumber, administrationOptions.Value);

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

        UserRole role = ResolveRole(isRoleManager, employee.IsGlobalManager, projectManagerOf, contractManagerOf);

        if (role >= UserRole.GlobalManager)
        {
            List<Guid> allProjectIds = await dbContext.Projects
                .AsNoTracking()
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            List<Guid> allContractIds = await dbContext.Contracts
                .AsNoTracking()
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            return new UserPermissions
            {
                EmployeeId = employee.Id,
                Role = role,
                ProjectManagerOf = projectManagerOf,
                ContractManagerOf = contractManagerOf,
                EmployeeOnContractIds = employeeOnContractIds,
                VisibleProjectIds = allProjectIds,
                VisibleContractIds = allContractIds,
            };
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

        return new UserPermissions
        {
            EmployeeId = employee.Id,
            Role = role,
            ProjectManagerOf = projectManagerOf,
            ContractManagerOf = contractManagerOf,
            EmployeeOnContractIds = employeeOnContractIds,
            VisibleProjectIds = visibleProjectIds.ToList(),
            VisibleContractIds = visibleContractIds.ToList(),
        };
    }

    private static UserRole ResolveRole(bool isRoleManager, bool isGlobalManager, IReadOnlyList<Guid> projectManagerOf, IReadOnlyList<Guid> contractManagerOf)
    {
        UserRole role = UserRole.Employee;
        if (contractManagerOf.Count > 0)
        {
            role = UserRole.ContractManager;
        }

        if (projectManagerOf.Count > 0)
        {
            role = UserRole.ProjectManager;
        }

        if (isGlobalManager)
        {
            role = UserRole.GlobalManager;
        }

        if (isRoleManager)
        {
            role = UserRole.Admin;
        }

        return role;
    }
}
