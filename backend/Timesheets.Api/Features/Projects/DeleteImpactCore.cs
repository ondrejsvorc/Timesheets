using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Features.Timesheets;

namespace Timesheets.Api.Features.Projects;

internal static class DeleteImpactCore
{
    public static async Task<bool> HasProtectedTimesheetsAsync(IReadOnlyList<Guid> contractIds, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        if (contractIds.Count == 0)
        {
            return false;
        }

        return await dbContext.ProjectTimesheets
            .AsNoTracking()
            .Where(t => contractIds.Contains(t.ContractId)
                && (t.TimesheetStatus.Code == TimesheetStatusCodes.Submitted
                    || t.TimesheetStatus.Code == TimesheetStatusCodes.Approved))
            .AnyAsync(cancellationToken);
    }
}
