using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Auth;
using Timesheets.Api.Data;

namespace Timesheets.Api.Timesheets;

internal static class ProjectTimesheetScopeLoader
{
    public static async Task<List<ProjectTimesheetPart>> LoadAsync(
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
                (timesheet, contractEmployee) => new { contractEmployee.ContractId })
            .Join(
                dbContext.Contracts.AsNoTracking(),
                x => x.ContractId,
                contract => contract.Id,
                (x, contract) => new ProjectTimesheetPart(x.ContractId, contract.ProjectId))
            .ToListAsync(cancellationToken);
    }
}
