namespace Timesheets.Api.Auth;

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
    bool Satisfies(UserRole minRole, Guid? projectId = null, Guid? contractId = null);
    Task<bool> CanAccessEmployeeAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<bool> CanViewAllContractTimesheetsAsync(Guid contractId, CancellationToken cancellationToken);
    bool CanManageProjectTimesheetParts(IReadOnlyList<ProjectTimesheetPart> parts);
}

public sealed record ProjectTimesheetPart(Guid ContractId, Guid ProjectId);
