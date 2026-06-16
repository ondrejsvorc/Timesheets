using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Timesheets;

namespace Timesheets.Api.Projects;

internal sealed record DeleteImpactCounts(
    int PositionCount,
    int DraftProjectTimesheetCount,
    int SubmittedProjectTimesheetCount,
    int ApprovedProjectTimesheetCount,
    int SubmittedAttendanceTimesheetCount,
    int ApprovedAttendanceTimesheetCount,
    bool HasProtectedTimesheets,
    bool CanDelete);

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
                && (t.TimesheetStatusId == TimesheetWorkflow.SubmittedStatusId
                    || t.TimesheetStatusId == TimesheetWorkflow.ApprovedStatusId))
            .AnyAsync(cancellationToken);
    }

    public static async Task<DeleteImpactCounts> CountAsync(IReadOnlyList<Guid> contractIds, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        int positionCount = contractIds.Count == 0
            ? 0
            : await dbContext.ContractEmployees
                .AsNoTracking()
                .CountAsync(ce => contractIds.Contains(ce.ContractId), cancellationToken);

        int draftCount = 0;
        int submittedCount = 0;
        int approvedCount = 0;
        int submittedAttendanceCount = 0;
        int approvedAttendanceCount = 0;

        if (contractIds.Count > 0)
        {
            draftCount = await dbContext.ProjectTimesheets
                .AsNoTracking()
                .CountAsync(
                    t => contractIds.Contains(t.ContractId) && t.TimesheetStatusId == TimesheetWorkflow.DraftStatusId,
                    cancellationToken);

            submittedCount = await dbContext.ProjectTimesheets
                .AsNoTracking()
                .CountAsync(
                    t => contractIds.Contains(t.ContractId) && t.TimesheetStatusId == TimesheetWorkflow.SubmittedStatusId,
                    cancellationToken);

            approvedCount = await dbContext.ProjectTimesheets
                .AsNoTracking()
                .CountAsync(
                    t => contractIds.Contains(t.ContractId) && t.TimesheetStatusId == TimesheetWorkflow.ApprovedStatusId,
                    cancellationToken);

            List<Guid> employeeIds = await dbContext.ContractEmployees
                .AsNoTracking()
                .Where(ce => contractIds.Contains(ce.ContractId))
                .Select(ce => ce.EmployeeId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (employeeIds.Count > 0)
            {
                submittedAttendanceCount = await dbContext.AttendanceTimesheets
                    .AsNoTracking()
                    .CountAsync(
                        t => employeeIds.Contains(t.EmployeeId) && t.TimesheetStatusId == TimesheetWorkflow.SubmittedStatusId,
                        cancellationToken);

                approvedAttendanceCount = await dbContext.AttendanceTimesheets
                    .AsNoTracking()
                    .CountAsync(
                        t => employeeIds.Contains(t.EmployeeId) && t.TimesheetStatusId == TimesheetWorkflow.ApprovedStatusId,
                        cancellationToken);
            }
        }

        bool hasProtected = submittedCount > 0 || approvedCount > 0;

        return new DeleteImpactCounts(
            PositionCount: positionCount,
            DraftProjectTimesheetCount: draftCount,
            SubmittedProjectTimesheetCount: submittedCount,
            ApprovedProjectTimesheetCount: approvedCount,
            SubmittedAttendanceTimesheetCount: submittedAttendanceCount,
            ApprovedAttendanceTimesheetCount: approvedAttendanceCount,
            HasProtectedTimesheets: hasProtected,
            CanDelete: !hasProtected);
    }
}
