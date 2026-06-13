using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
using Timesheets.Api.Common;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Auth;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor, AppDbContext dbContext, IOptions<AdministrationOptions> administrationOptions) : ICurrentUser
{
    private UserPermissions? _permissions;
    private UserPermissions Permissions => _permissions ?? throw new InvalidOperationException("Current user is not loaded.");

    public Guid EmployeeId => Permissions.EmployeeId;
    public UserRole Role => Permissions.Role;
    public IReadOnlyList<Guid> ProjectManagerOf => Permissions.ProjectManagerOf;
    public IReadOnlyList<Guid> ContractManagerOf => Permissions.ContractManagerOf;
    public IReadOnlyList<Guid> EmployeeOnContractIds => Permissions.EmployeeOnContractIds;
    public IReadOnlyList<Guid> VisibleProjectIds => Permissions.VisibleProjectIds;
    public IReadOnlyList<Guid> VisibleContractIds => Permissions.VisibleContractIds;

    internal async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_permissions is not null)
        {
            return;
        }

        HttpContext httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");
        Employee employee = await CurrentEmployeeResolver.GetRequiredAsync(httpContext.User, dbContext, cancellationToken);
        _permissions = await UserPermissionsLoader.LoadAsync(employee, dbContext, administrationOptions, cancellationToken);
    }

    public bool IsAtLeast(UserRole role) => Role >= role;

    public bool Satisfies(UserRole minRole, Guid? projectId = null, Guid? contractId = null)
    {
        if (!IsAtLeast(minRole))
        {
            return false;
        }

        if (IsAtLeast(UserRole.GlobalManager))
        {
            return true;
        }

        if (projectId is not null && !VisibleProjectIds.Contains(projectId.Value))
        {
            return false;
        }

        if (contractId is not null && !VisibleContractIds.Contains(contractId.Value))
        {
            return false;
        }

        return true;
    }

    public async Task<bool> CanAccessEmployeeAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        if (EmployeeId == employeeId)
        {
            return true;
        }

        if (!IsAtLeast(UserRole.ContractManager))
        {
            return false;
        }

        if (IsAtLeast(UserRole.GlobalManager))
        {
            return await dbContext.Employees.AsNoTracking().AnyAsync(e => e.Id == employeeId, cancellationToken);
        }

        HashSet<Guid> visibleContractIds = VisibleContractIds.ToHashSet();
        HashSet<Guid> visibleProjectIds = VisibleProjectIds.ToHashSet();

        return await dbContext.ContractEmployees
            .AsNoTracking()
            .Where(ce => ce.EmployeeId == employeeId)
            .AnyAsync(ce => visibleContractIds.Contains(ce.ContractId) || visibleProjectIds.Contains(ce.Contract.ProjectId), cancellationToken);
    }

    public async Task<bool> CanViewAllContractTimesheetsAsync(Guid contractId, CancellationToken cancellationToken)
    {
        if (IsAtLeast(UserRole.GlobalManager))
        {
            return true;
        }

        if (IsAtLeast(UserRole.ContractManager) && VisibleContractIds.Contains(contractId))
        {
            return true;
        }

        if (!IsAtLeast(UserRole.ProjectManager))
        {
            return false;
        }

        Guid? projectId = await dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.Id == contractId)
            .Select(c => (Guid?)c.ProjectId)
            .FirstOrDefaultAsync(cancellationToken);

        return projectId.HasValue && ProjectManagerOf.Contains(projectId.Value);
    }

    public bool CanManageProjectTimesheetParts(IReadOnlyList<ProjectTimesheetPart> parts)
    {
        if (IsAtLeast(UserRole.GlobalManager))
        {
            return true;
        }

        return parts.All(part =>
            (IsAtLeast(UserRole.ContractManager) && VisibleContractIds.Contains(part.ContractId))
            || (IsAtLeast(UserRole.ProjectManager) && ProjectManagerOf.Contains(part.ProjectId)));
    }
}
