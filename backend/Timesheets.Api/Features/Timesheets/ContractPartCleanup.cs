using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;

namespace Timesheets.Api.Features.Timesheets;

internal static class ContractPartCleanup
{
    public static async Task<bool> HasProtectedPartsForAssignmentAsync(Guid contractEmployeeId, AppDbContext dbContext, CancellationToken cancellationToken) =>
        await dbContext.ContractParts
            .AsNoTracking()
            .AnyAsync(
                part => part.ContractEmployeeId == contractEmployeeId
                    && (part.TimesheetStatus.Code == TimesheetStatusCodes.Submitted || part.TimesheetStatus.Code == TimesheetStatusCodes.Approved),
                cancellationToken);

    public static async Task RemoveDraftPartsForContractIdsAsync(IReadOnlyList<Guid> contractIds, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        if (contractIds.Count == 0)
        {
            return;
        }

        List<Guid> assignmentIds = await dbContext.ContractEmployees
            .AsNoTracking()
            .Where(assignment => contractIds.Contains(assignment.ContractId))
            .Select(assignment => assignment.Id)
            .ToListAsync(cancellationToken);

        foreach (Guid assignmentId in assignmentIds)
        {
            await RemoveDraftPartsForAssignmentAsync(assignmentId, dbContext, cancellationToken);
        }
    }

    public static async Task RemoveDraftPartsForAssignmentAsync(Guid contractEmployeeId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        List<Guid> draftPartIds = await dbContext.ContractParts
            .Where(part => part.ContractEmployeeId == contractEmployeeId && part.TimesheetStatus.Code == TimesheetStatusCodes.Draft)
            .Select(part => part.Id)
            .ToListAsync(cancellationToken);

        if (draftPartIds.Count == 0)
        {
            return;
        }

        HashSet<Guid> timesheetIds = await dbContext.ContractParts
            .AsNoTracking()
            .Where(part => draftPartIds.Contains(part.Id))
            .Select(part => part.TimesheetId)
            .ToHashSetAsync(cancellationToken);

        await dbContext.ContractParts
            .Where(part => draftPartIds.Contains(part.Id))
            .ExecuteDeleteAsync(cancellationToken);

        await CleanupEmptyDraftTimesheetsAsync(timesheetIds, dbContext, cancellationToken);
    }

    private static async Task CleanupEmptyDraftTimesheetsAsync(HashSet<Guid> timesheetIds, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        foreach (Guid timesheetId in timesheetIds)
        {
            bool hasParts = await dbContext.ContractParts.AnyAsync(part => part.TimesheetId == timesheetId, cancellationToken);
            if (hasParts)
            {
                continue;
            }

            bool hasAttendance = await dbContext.Attendances.AnyAsync(attendance => attendance.TimesheetId == timesheetId, cancellationToken);
            if (hasAttendance)
            {
                continue;
            }

            bool isDraft = await dbContext.Timesheets
                .AsNoTracking()
                .AnyAsync(
                    timesheet => timesheet.Id == timesheetId && timesheet.TimesheetStatus.Code == TimesheetStatusCodes.Draft,
                    cancellationToken);

            if (!isDraft)
            {
                continue;
            }

            await dbContext.Timesheets
                .Where(timesheet => timesheet.Id == timesheetId)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
