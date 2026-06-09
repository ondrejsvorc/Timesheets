using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
using Timesheets.Api.Auth;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Timesheets;

internal sealed record EmployeeWorkflowPermissions(
    Guid EmployeeId,
    bool HasGlobalScope,
    HashSet<Guid> ProjectManagerOf,
    HashSet<Guid> ContractManagerOf)
{
    public bool CanManageProjectPart(Guid contractId, Guid projectId) =>
        HasGlobalScope
        || ContractManagerOf.Contains(contractId)
        || ProjectManagerOf.Contains(projectId);
}

internal sealed record ProjectTimesheetScope(Guid ProjectTimesheetId, Guid ContractId, Guid ProjectId);

internal static class TimesheetWorkflowAuthorization
{
    public static async Task<EmployeeWorkflowPermissions> LoadAsync(
        Employee employee,
        AppDbContext dbContext,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        UserPermissionsScope scope = await UserPermissionsScopeLoader.LoadAsync(employee, dbContext, administrationOptions, cancellationToken)
            ?? throw new InvalidOperationException("Employee permissions scope was not found.");

        return new EmployeeWorkflowPermissions(
            scope.EmployeeId,
            scope.HasGlobalScope,
            scope.ProjectManagerOf.ToHashSet(),
            scope.ContractManagerOf.ToHashSet());
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
        permissions.HasGlobalScope;

    public static bool CanManageProjectTimesheets(
        EmployeeWorkflowPermissions permissions,
        IEnumerable<ProjectTimesheetScope> projectScopes)
    {
        if (permissions.HasGlobalScope)
        {
            return true;
        }

        return projectScopes.All(scope => permissions.CanManageProjectPart(scope.ContractId, scope.ProjectId));
    }
}
