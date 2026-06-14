using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects;

public sealed record ProjectDeleteImpact(
    int ContractCount,
    int PositionCount,
    int DraftProjectTimesheetCount,
    int SubmittedProjectTimesheetCount,
    int ApprovedProjectTimesheetCount,
    bool HasProtectedTimesheets,
    bool CanDelete,
    bool CanForceDelete);

internal static class ProjectDeleteImpactCalculator
{
    public static async Task<ProjectDeleteImpact?> ForProjectAsync(Guid projectId, bool canForceDelete, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        bool exists = await dbContext.Projects.AsNoTracking().AnyAsync(p => p.Id == projectId, cancellationToken);
        if (!exists)
        {
            return null;
        }

        List<Guid> contractIds = await dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.ProjectId == projectId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        DeleteImpactCounts counts = await DeleteImpactCore.CountAsync(contractIds, canForceDelete, dbContext, cancellationToken);

        return new ProjectDeleteImpact(
            ContractCount: contractIds.Count,
            PositionCount: counts.PositionCount,
            DraftProjectTimesheetCount: counts.DraftProjectTimesheetCount,
            SubmittedProjectTimesheetCount: counts.SubmittedProjectTimesheetCount,
            ApprovedProjectTimesheetCount: counts.ApprovedProjectTimesheetCount,
            HasProtectedTimesheets: counts.HasProtectedTimesheets,
            CanDelete: counts.CanDelete,
            CanForceDelete: counts.CanForceDelete);
    }
}
