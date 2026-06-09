using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Timesheets;

internal sealed record EmployeeWorkflowPermissions(
    Guid EmployeeId,
    bool IsGlobalManager,
    HashSet<Guid> ProjectManagerOf,
    HashSet<Guid> ContractManagerOf)
{
    public bool CanManageProjectPart(Guid contractId, Guid projectId) =>
        IsGlobalManager
        || ContractManagerOf.Contains(contractId)
        || ProjectManagerOf.Contains(projectId);
}

internal sealed record ProjectTimesheetScope(Guid ProjectTimesheetId, Guid ContractId, Guid ProjectId);

internal static class TimesheetWorkflowAuthorization
{
    public static async Task<EmployeeWorkflowPermissions> LoadAsync(
        Employee employee,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
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

        return new EmployeeWorkflowPermissions(
            employee.Id,
            employee.IsGlobalManager,
            projectManagerOf.ToHashSet(),
            contractManagerOf.ToHashSet());
    }

    public static async Task<List<ProjectTimesheetScope>> LoadProjectScopesAsync(
        IEnumerable<Guid> projectTimesheetIds,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return await dbContext.ProjectTimesheets
            .AsNoTracking()
            .Where(timesheet => projectTimesheetIds.Contains(timesheet.Id))
            .Join(
                dbContext.ContractEmployees.AsNoTracking(),
                timesheet => timesheet.ContractEmployeeId,
                contractEmployee => contractEmployee.Id,
                (timesheet, contractEmployee) => new { timesheet.Id, contractEmployee.ContractId })
            .Join(
                dbContext.Contracts.AsNoTracking(),
                x => x.ContractId,
                contract => contract.Id,
                (x, contract) => new ProjectTimesheetScope(x.Id, x.ContractId, contract.ProjectId))
            .ToListAsync(cancellationToken);
    }

    public static bool CanSubmitTimesheet(EmployeeWorkflowPermissions permissions, Guid timesheetOwnerId) =>
        permissions.EmployeeId == timesheetOwnerId;

    public static bool CanManageWholeTimesheet(EmployeeWorkflowPermissions permissions) =>
        permissions.IsGlobalManager;

    public static bool CanManageProjectTimesheets(
        EmployeeWorkflowPermissions permissions,
        IEnumerable<ProjectTimesheetScope> projectScopes)
    {
        if (permissions.IsGlobalManager)
        {
            return true;
        }

        return projectScopes.All(scope => permissions.CanManageProjectPart(scope.ContractId, scope.ProjectId));
    }
}
