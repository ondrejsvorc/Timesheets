using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Timesheets;

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
    public static async Task<ProjectDeleteImpact?> ForProjectAsync(
        Guid projectId,
        bool canForceDelete,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
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

        return await BuildAsync(contractIds, canForceDelete, dbContext, cancellationToken);
    }

    public static async Task<ProjectDeleteImpact?> ForContractAsync(
        Guid projectId,
        Guid contractId,
        bool canForceDelete,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        bool exists = await dbContext.Contracts
            .AsNoTracking()
            .AnyAsync(c => c.ProjectId == projectId && c.Id == contractId, cancellationToken);

        if (!exists)
        {
            return null;
        }

        return await BuildAsync([contractId], canForceDelete, dbContext, cancellationToken);
    }

    public static async Task<bool> HasProtectedTimesheetsAsync(
        IReadOnlyList<Guid> contractIds,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (contractIds.Count == 0)
        {
            return false;
        }

        return await dbContext.ProjectTimesheets
            .AsNoTracking()
            .Where(t => contractIds.Contains(t.ContractId)
                && (t.TimesheetStatusId == TimesheetWorkflowConstants.SubmittedStatusId
                    || t.TimesheetStatusId == TimesheetWorkflowConstants.ApprovedStatusId))
            .AnyAsync(cancellationToken);
    }

    private static async Task<ProjectDeleteImpact> BuildAsync(
        IReadOnlyList<Guid> contractIds,
        bool canForceDelete,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        int positionCount = contractIds.Count == 0
            ? 0
            : await dbContext.ContractEmployees
                .AsNoTracking()
                .CountAsync(ce => contractIds.Contains(ce.ContractId), cancellationToken);

        int draftCount = 0;
        int submittedCount = 0;
        int approvedCount = 0;

        if (contractIds.Count > 0)
        {
            draftCount = await dbContext.ProjectTimesheets
                .AsNoTracking()
                .CountAsync(
                    t => contractIds.Contains(t.ContractId) && t.TimesheetStatusId == TimesheetWorkflowConstants.DraftStatusId,
                    cancellationToken);

            submittedCount = await dbContext.ProjectTimesheets
                .AsNoTracking()
                .CountAsync(
                    t => contractIds.Contains(t.ContractId) && t.TimesheetStatusId == TimesheetWorkflowConstants.SubmittedStatusId,
                    cancellationToken);

            approvedCount = await dbContext.ProjectTimesheets
                .AsNoTracking()
                .CountAsync(
                    t => contractIds.Contains(t.ContractId) && t.TimesheetStatusId == TimesheetWorkflowConstants.ApprovedStatusId,
                    cancellationToken);
        }

        bool hasProtected = submittedCount > 0 || approvedCount > 0;

        return new ProjectDeleteImpact(
            ContractCount: contractIds.Count,
            PositionCount: positionCount,
            DraftProjectTimesheetCount: draftCount,
            SubmittedProjectTimesheetCount: submittedCount,
            ApprovedProjectTimesheetCount: approvedCount,
            HasProtectedTimesheets: hasProtected,
            CanDelete: !hasProtected,
            CanForceDelete: hasProtected && canForceDelete);
    }
}
