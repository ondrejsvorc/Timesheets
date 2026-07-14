using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;
using Timesheets.Api.Domain.Models;

namespace Timesheets.Api.Features.Projects;

internal static class DeleteImpactCore
{
    public static async Task<bool> HasProtectedTimesheetsAsync(IReadOnlyList<Guid> contractIds, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        if (contractIds.Count == 0)
        {
            return false;
        }

        return await dbContext.ContractParts
            .AsNoTracking()
            .Where(t => contractIds.Contains(t.ContractEmployee.ContractId)
                && (t.TimesheetStatus.Code == TimesheetStatus.SubmittedCode
                    || t.TimesheetStatus.Code == TimesheetStatus.ApprovedCode))
            .AnyAsync(cancellationToken);
    }
}
