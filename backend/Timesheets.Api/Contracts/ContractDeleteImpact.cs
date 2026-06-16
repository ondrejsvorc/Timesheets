using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Projects;

namespace Timesheets.Api.Contracts;

public sealed record ContractDeleteImpact(
    int PositionCount,
    int DraftProjectTimesheetCount,
    int SubmittedProjectTimesheetCount,
    int ApprovedProjectTimesheetCount,
    int SubmittedAttendanceTimesheetCount,
    int ApprovedAttendanceTimesheetCount,
    bool HasProtectedTimesheets,
    bool CanDelete);

internal static class ContractDeleteImpactCalculator
{
    public static async Task<ContractDeleteImpact?> ForContractAsync(Guid contractId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        bool exists = await dbContext.Contracts
            .AsNoTracking()
            .AnyAsync(c => c.Id == contractId, cancellationToken);

        if (!exists)
        {
            return null;
        }

        DeleteImpactCounts counts = await DeleteImpactCore.CountAsync([contractId], dbContext, cancellationToken);

        return new ContractDeleteImpact(
            PositionCount: counts.PositionCount,
            DraftProjectTimesheetCount: counts.DraftProjectTimesheetCount,
            SubmittedProjectTimesheetCount: counts.SubmittedProjectTimesheetCount,
            ApprovedProjectTimesheetCount: counts.ApprovedProjectTimesheetCount,
            SubmittedAttendanceTimesheetCount: counts.SubmittedAttendanceTimesheetCount,
            ApprovedAttendanceTimesheetCount: counts.ApprovedAttendanceTimesheetCount,
            HasProtectedTimesheets: counts.HasProtectedTimesheets,
            CanDelete: counts.CanDelete);
    }
}
