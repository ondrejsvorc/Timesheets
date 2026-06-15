using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;
using Timesheets.Api.Timesheets;

namespace Timesheets.Api.Contracts;

public sealed record ContractEmployeeUpdateRequest(
    string PositionCode,
    string Position,
    decimal Workload,
    DateTime StartDate,
    DateTime? EndDate);

public sealed record ContractEmployeeUpdateImpact(
    bool CanUpdate,
    bool CreatesNewAssignment,
    string? BlockReason,
    DateTime? CurrentAssignmentEndDate,
    DateTime? NewAssignmentStartDate,
    int NewTimesheetMonthCount,
    int DraftTimesheetsOnOldAssignment,
    int DraftDaysToRemove,
    int SubmittedTimesheetCount,
    int ApprovedTimesheetCount);

internal enum ContractEmployeeUpdateMode
{
    Split,
    ShortenEnd,
    ExtendEnd,
}

internal static class ContractEmployeeUpdatePlanner
{
    public static bool IsUnchanged(ContractEmployee existing, ContractEmployeeUpdateRequest request) =>
        existing.PositionCode == request.PositionCode
        && existing.Position == request.Position
        && existing.Workload == request.Workload
        && ContractEmployeeValidation.ToUtcDate(existing.StartDate) == ContractEmployeeValidation.ToUtcDate(request.StartDate)
        && NullableDatesEqual(existing.EndDate, request.EndDate);

    public static async Task<ContractEmployeeUpdateImpact> PlanAsync(ContractEmployee existing, ContractEmployeeUpdateRequest request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var projectRange = await dbContext.Contracts
            .AsNoTracking()
            .Where(contract => contract.Id == existing.ContractId)
            .Select(contract => new { contract.Project.StartDate, contract.Project.EndDate })
            .SingleAsync(cancellationToken);

        string? projectRangeError = ContractEmployeeValidation.ValidateProjectRange(projectRange.StartDate, projectRange.EndDate, request.StartDate, request.EndDate);
        if (projectRangeError is not null)
        {
            return Blocked(projectRangeError);
        }

        if (IsUnchanged(existing, request))
        {
            return Blocked("Nebyla zadána žádná změna.");
        }

        DateTime existingStart = ContractEmployeeValidation.ToUtcDate(existing.StartDate);
        DateTime newStart = ContractEmployeeValidation.ToUtcDate(request.StartDate);
        DateTime? existingEnd = existing.EndDate.HasValue ? ContractEmployeeValidation.ToUtcDate(existing.EndDate.Value) : null;
        DateTime? newEnd = request.EndDate.HasValue ? ContractEmployeeValidation.ToUtcDate(request.EndDate.Value) : null;

        if (newStart < existingStart)
        {
            return Blocked("Začátek pozice nelze posunout do minulosti.");
        }

        if (newEnd.HasValue && newEnd.Value < newStart)
        {
            return Blocked("Datum ukončení musí být po datu začátku.");
        }

        bool metadataSame = existing.PositionCode == request.PositionCode
            && existing.Position == request.Position
            && existing.Workload == request.Workload;

        if (metadataSame && newStart == existingStart)
        {
            if (IsShorteningEnd(existingEnd, newEnd))
            {
                return await PlanShortenEndAsync(existing, newEnd!.Value, dbContext, cancellationToken);
            }

            if (IsExtendingEnd(existingEnd, newEnd))
            {
                return await PlanExtendEndAsync(existing, newEnd, dbContext, cancellationToken);
            }
        }

        if (newStart <= existingStart)
        {
            return Blocked("Nový záznam musí začít po datu začátku stávající pozice, nebo upravte jen datum ukončení.");
        }

        return await PlanSplitAsync(existing, newStart, newEnd ?? newStart, dbContext, cancellationToken);
    }

    private static bool IsShorteningEnd(DateTime? existingEnd, DateTime? newEnd)
    {
        if (!newEnd.HasValue)
        {
            return false;
        }

        if (!existingEnd.HasValue)
        {
            return true;
        }

        return newEnd.Value < existingEnd.Value;
    }

    private static bool IsExtendingEnd(DateTime? existingEnd, DateTime? newEnd)
    {
        if (existingEnd is null)
        {
            return newEnd.HasValue;
        }

        if (!newEnd.HasValue)
        {
            return true;
        }

        return newEnd.Value > existingEnd.Value;
    }

    private static async Task<ContractEmployeeUpdateImpact> PlanShortenEndAsync(ContractEmployee existing, DateTime newEnd, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        (int submitted, int approved, int draftDaysOutside) = await AnalyzeProtectedOutsideRangeAsync(
            existing.Id,
            newEnd,
            dbContext,
            cancellationToken);

        if (submitted > 0 || approved > 0)
        {
            return Blocked("Nelze zkrátit období — existují výkazy ke schválení nebo schválené mimo nový rozsah.");
        }

        (int draft, int submittedOnAssignment, int approvedOnAssignment) = await CountTimesheetsOnAssignmentAsync(
            existing.Id,
            dbContext,
            cancellationToken);

        return new ContractEmployeeUpdateImpact(
            CanUpdate: true,
            CreatesNewAssignment: false,
            BlockReason: null,
            CurrentAssignmentEndDate: newEnd,
            NewAssignmentStartDate: null,
            NewTimesheetMonthCount: 0,
            DraftTimesheetsOnOldAssignment: draft,
            DraftDaysToRemove: draftDaysOutside,
            SubmittedTimesheetCount: submittedOnAssignment,
            ApprovedTimesheetCount: approvedOnAssignment);
    }

    private static async Task<ContractEmployeeUpdateImpact> PlanExtendEndAsync(ContractEmployee existing, DateTime? newEnd, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        DateTime rangeStart = ContractEmployeeValidation.ToUtcDate(existing.StartDate);
        DateTime rangeEnd = newEnd ?? rangeStart;
        int newMonths = CountMonths(rangeStart, rangeEnd);

        (int draft, int submitted, int approved) = await CountTimesheetsOnAssignmentAsync(
            existing.Id,
            dbContext,
            cancellationToken);

        return new ContractEmployeeUpdateImpact(
            CanUpdate: true,
            CreatesNewAssignment: false,
            BlockReason: null,
            CurrentAssignmentEndDate: newEnd,
            NewAssignmentStartDate: null,
            NewTimesheetMonthCount: newMonths,
            DraftTimesheetsOnOldAssignment: draft,
            DraftDaysToRemove: 0,
            SubmittedTimesheetCount: submitted,
            ApprovedTimesheetCount: approved);
    }

    private static async Task<ContractEmployeeUpdateImpact> PlanSplitAsync(ContractEmployee existing, DateTime newStart, DateTime newEnd, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        DateTime existingStart = ContractEmployeeValidation.ToUtcDate(existing.StartDate);
        DateTime oldEnd = newStart.AddDays(-1);
        if (oldEnd < existingStart)
        {
            return Blocked("Nový záznam musí začít po datu začátku stávající pozice.");
        }

        (int draft, int submitted, int approved) = await CountTimesheetsOnAssignmentAsync(
            existing.Id,
            dbContext,
            cancellationToken);

        return new ContractEmployeeUpdateImpact(
            CanUpdate: true,
            CreatesNewAssignment: true,
            BlockReason: null,
            CurrentAssignmentEndDate: oldEnd,
            NewAssignmentStartDate: newStart,
            NewTimesheetMonthCount: CountMonths(newStart, newEnd),
            DraftTimesheetsOnOldAssignment: draft,
            DraftDaysToRemove: 0,
            SubmittedTimesheetCount: submitted,
            ApprovedTimesheetCount: approved);
    }

    private static async Task<(int Submitted, int Approved, int DraftDaysOutside)> AnalyzeProtectedOutsideRangeAsync(Guid contractEmployeeId, DateTime newEnd, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        List<Data.Models.ProjectTimesheet> timesheets = await dbContext.ProjectTimesheets
            .AsNoTracking()
            .Include(t => t.Days)
            .Where(t => t.ContractEmployeeId == contractEmployeeId)
            .ToListAsync(cancellationToken);

        HashSet<Guid> submittedOutside = [];
        HashSet<Guid> approvedOutside = [];
        int draftDaysOutside = 0;

        foreach (Data.Models.ProjectTimesheet timesheet in timesheets)
        {
            bool isSubmitted = timesheet.TimesheetStatusId == TimesheetWorkflow.SubmittedStatusId;
            bool isApproved = timesheet.TimesheetStatusId == TimesheetWorkflow.ApprovedStatusId;
            bool isDraft = timesheet.TimesheetStatusId == TimesheetWorkflow.DraftStatusId;

            foreach (Data.Models.ProjectDay day in timesheet.Days)
            {
                if (ContractEmployeeValidation.ToUtcDate(day.Date) <= newEnd)
                {
                    continue;
                }

                if (isSubmitted)
                {
                    submittedOutside.Add(timesheet.Id);
                }
                else if (isApproved)
                {
                    approvedOutside.Add(timesheet.Id);
                }
                else if (isDraft)
                {
                    draftDaysOutside++;
                }
            }
        }

        return (submittedOutside.Count, approvedOutside.Count, draftDaysOutside);
    }

    private static ContractEmployeeUpdateImpact Blocked(string reason) =>
        new(
            CanUpdate: false,
            CreatesNewAssignment: false,
            BlockReason: reason,
            CurrentAssignmentEndDate: null,
            NewAssignmentStartDate: null,
            NewTimesheetMonthCount: 0,
            DraftTimesheetsOnOldAssignment: 0,
            DraftDaysToRemove: 0,
            SubmittedTimesheetCount: 0,
            ApprovedTimesheetCount: 0);

    private static bool NullableDatesEqual(DateTime? left, DateTime? right)
    {
        if (!left.HasValue && !right.HasValue)
        {
            return true;
        }

        if (!left.HasValue || !right.HasValue)
        {
            return false;
        }

        return ContractEmployeeValidation.ToUtcDate(left.Value) == ContractEmployeeValidation.ToUtcDate(right.Value);
    }

    private static int CountMonths(DateTime start, DateTime end)
    {
        DateTime cursor = new(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime last = new(end.Year, end.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        int count = 0;
        while (cursor <= last)
        {
            count++;
            cursor = cursor.AddMonths(1);
        }

        return count;
    }

    private static async Task<(int Draft, int Submitted, int Approved)> CountTimesheetsOnAssignmentAsync(Guid contractEmployeeId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        List<Guid> statusIds = await dbContext.ProjectTimesheets
            .AsNoTracking()
            .Where(t => t.ContractEmployeeId == contractEmployeeId)
            .Select(t => t.TimesheetStatusId)
            .ToListAsync(cancellationToken);

        return (
            statusIds.Count(id => id == TimesheetWorkflow.DraftStatusId),
            statusIds.Count(id => id == TimesheetWorkflow.SubmittedStatusId),
            statusIds.Count(id => id == TimesheetWorkflow.ApprovedStatusId));
    }
}
