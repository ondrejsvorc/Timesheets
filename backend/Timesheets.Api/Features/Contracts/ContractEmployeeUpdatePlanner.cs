using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;
using Timesheets.Api.Domain.Models;

namespace Timesheets.Api.Features.Contracts;

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
    public static bool IsUnchanged(ContractEmployee existing, ContractEmployeeUpdateRequest request, DateTime? projectEndDate) =>
        existing.PositionCode == request.PositionCode
        && existing.Position == request.Position
        && existing.Workload == request.Workload
        && ContractEmployeeValidation.ToUtcDate(existing.StartDate) == ContractEmployeeValidation.ToUtcDate(request.StartDate)
        && EffectiveEndsEqual(existing.EndDate, request.EndDate, projectEndDate);

    public static async Task<ContractEmployeeUpdateImpact> PlanAsync(ContractEmployee existing, ContractEmployeeUpdateRequest request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Project project = await dbContext.Contracts
            .AsNoTracking()
            .Where(contract => contract.Id == existing.ContractId)
            .Select(contract => contract.Project)
            .SingleAsync(cancellationToken);

        string? projectRangeError = ContractEmployeeValidation.ValidateProjectRange(project, request.StartDate, request.EndDate);
        if (projectRangeError is not null)
        {
            return Blocked(projectRangeError);
        }

        if (!request.EndDate.HasValue && project.EndDate.HasValue)
        {
            request = request with { EndDate = project.EndDate };
        }

        DateTime? projectEnd = project.EndDate;

        if (IsUnchanged(existing, request, projectEnd))
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

        bool endsChanged = !EffectiveEndsEqual(existing.EndDate, request.EndDate, projectEnd);

        if (newStart == existingStart)
        {
            if (!endsChanged && !metadataSame)
            {
                return await PlanMetadataOnlyAsync(existing, dbContext, cancellationToken);
            }

            if (IsShorteningEnd(existingEnd, newEnd, projectEnd))
            {
                return await PlanShortenEndAsync(existing, newEnd!.Value, dbContext, cancellationToken);
            }

            if (IsExtendingEnd(existingEnd, newEnd, projectEnd))
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

    private static bool IsShorteningEnd(DateTime? existingEnd, DateTime? newEnd, DateTime? projectEnd)
    {
        DateTime? effectiveExisting = EffectiveEnd(existingEnd, projectEnd);
        DateTime? effectiveNew = EffectiveEnd(newEnd, projectEnd);
        if (!effectiveNew.HasValue || !effectiveExisting.HasValue)
        {
            return false;
        }

        return effectiveNew.Value < effectiveExisting.Value;
    }

    private static bool IsExtendingEnd(DateTime? existingEnd, DateTime? newEnd, DateTime? projectEnd)
    {
        DateTime? effectiveExisting = EffectiveEnd(existingEnd, projectEnd);
        DateTime? effectiveNew = EffectiveEnd(newEnd, projectEnd);
        if (!effectiveNew.HasValue)
        {
            return effectiveExisting.HasValue;
        }

        if (!effectiveExisting.HasValue)
        {
            return false;
        }

        return effectiveNew.Value > effectiveExisting.Value;
    }

    private static Task<ContractEmployeeUpdateImpact> PlanMetadataOnlyAsync(ContractEmployee existing, AppDbContext dbContext, CancellationToken cancellationToken) =>
        Task.FromResult(new ContractEmployeeUpdateImpact(
            CanUpdate: true,
            CreatesNewAssignment: false,
            BlockReason: null,
            CurrentAssignmentEndDate: null,
            NewAssignmentStartDate: null,
            NewTimesheetMonthCount: 0,
            DraftTimesheetsOnOldAssignment: 0,
            DraftDaysToRemove: 0,
            SubmittedTimesheetCount: 0,
            ApprovedTimesheetCount: 0));

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
        List<Domain.Models.ContractPart> timesheets = await dbContext.ContractParts
            .AsNoTracking()
            .Include(t => t.TimesheetStatus)
            .Include(t => t.Days)
            .Where(t => t.ContractEmployeeId == contractEmployeeId)
            .ToListAsync(cancellationToken);

        HashSet<Guid> submittedOutside = [];
        HashSet<Guid> approvedOutside = [];
        int draftDaysOutside = 0;

        foreach (Domain.Models.ContractPart timesheet in timesheets)
        {
            bool isSubmitted = timesheet.TimesheetStatus.IsSubmitted;
            bool isApproved = timesheet.TimesheetStatus.IsApproved;
            bool isDraft = timesheet.TimesheetStatus.IsDraft;

            foreach (Domain.Models.ContractPartDay day in timesheet.Days)
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

    private static DateTime? EffectiveEnd(DateTime? end, DateTime? projectEnd) =>
        end.HasValue
            ? ContractEmployeeValidation.ToUtcDate(end.Value)
            : projectEnd.HasValue
                ? ContractEmployeeValidation.ToUtcDate(projectEnd.Value)
                : null;

    private static bool EffectiveEndsEqual(DateTime? left, DateTime? right, DateTime? projectEnd) =>
        EffectiveEnd(left, projectEnd) == EffectiveEnd(right, projectEnd);

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
        List<string> statusCodes = await dbContext.ContractParts
            .AsNoTracking()
            .Where(t => t.ContractEmployeeId == contractEmployeeId)
            .Select(t => t.TimesheetStatus.Code)
            .ToListAsync(cancellationToken);

        return (
            statusCodes.Count(code => code == TimesheetStatus.DraftCode),
            statusCodes.Count(code => code == TimesheetStatus.SubmittedCode),
            statusCodes.Count(code => code == TimesheetStatus.ApprovedCode));
    }
}
