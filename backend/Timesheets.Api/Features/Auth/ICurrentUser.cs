namespace Timesheets.Api.Features.Auth;

public interface ICurrentUser
{
    Guid EmployeeId { get; }
    UserRole Role { get; }
    IReadOnlyList<Guid> ProjectManagerOf { get; }
    IReadOnlyList<Guid> ContractManagerOf { get; }
    IReadOnlyList<Guid> EmployeeOnContractIds { get; }
    IReadOnlyList<Guid> VisibleProjectIds { get; }
    IReadOnlyList<Guid> VisibleContractIds { get; }

    bool IsAtLeast(UserRole role);
    bool IsContractManager() => IsAtLeast(UserRole.ContractManager);
    bool IsProjectManager() => IsAtLeast(UserRole.ProjectManager);
    bool IsGlobalManagerRole() => IsAtLeast(UserRole.GlobalManager);
    bool IsAdmin() => IsAtLeast(UserRole.Admin);
    bool CanManageProject(Guid projectId) => IsGlobalManagerRole() || ProjectManagerOf.Contains(projectId);
    bool CanManageContract(Guid contractId, Guid projectId) => IsGlobalManagerRole() || ProjectManagerOf.Contains(projectId) || ContractManagerOf.Contains(contractId);
    bool Satisfies(UserRole minRole, Guid? projectId = null, Guid? contractId = null);
    Task<bool> CanAccessEmployeeAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<bool> CanViewAllContractTimesheetsAsync(Guid contractId, CancellationToken cancellationToken);
    bool CanManageContractPartScopes(IReadOnlyList<ContractPartScope> parts);
}

public sealed record ContractPartScope(Guid ContractId, Guid ProjectId);
