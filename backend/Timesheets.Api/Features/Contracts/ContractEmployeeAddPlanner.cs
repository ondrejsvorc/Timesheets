using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Features.Timesheets;

namespace Timesheets.Api.Features.Contracts;

public sealed record ContractEmployeeAddRequest(Guid EmployeeId, DateTime StartDate, DateTime? EndDate);

public sealed record ContractEmployeeAddImpact(
    bool CanAdd,
    string? BlockReason,
    int SubmittedTimesheetCount,
    int ApprovedTimesheetCount);

internal static class ContractEmployeeAddPlanner
{
    public static async Task<ContractEmployeeAddImpact> PlanAsync(Guid contractId, DateTime? projectEndDate, ContractEmployeeAddRequest request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        DateTime start = ContractEmployeeValidation.ToUtcDate(request.StartDate);
        DateTime? end = request.EndDate.HasValue
            ? ContractEmployeeValidation.ToUtcDate(request.EndDate.Value)
            : projectEndDate.HasValue ? ContractEmployeeValidation.ToUtcDate(projectEndDate.Value) : null;

        int startKey = start.Year * 100 + start.Month;
        int? endKey = end.HasValue ? end.Value.Year * 100 + end.Value.Month : null;

        List<Guid> statusIds = await dbContext.ProjectTimesheets
            .AsNoTracking()
            .Where(t => t.ContractId == contractId && t.EmployeeId == request.EmployeeId)
            .Where(t => (t.Year * 100 + t.Month) >= startKey)
            .Where(t => endKey == null || (t.Year * 100 + t.Month) <= endKey.Value)
            .Where(t => t.TimesheetStatusId == TimesheetWorkflow.SubmittedStatusId || t.TimesheetStatusId == TimesheetWorkflow.ApprovedStatusId)
            .Select(t => t.TimesheetStatusId)
            .ToListAsync(cancellationToken);

        int submitted = statusIds.Count(id => id == TimesheetWorkflow.SubmittedStatusId);
        int approved = statusIds.Count(id => id == TimesheetWorkflow.ApprovedStatusId);

        if (submitted > 0 || approved > 0)
        {
            return new ContractEmployeeAddImpact(
                CanAdd: false,
                BlockReason: "Pozici nelze přidat — v období existují výkazy ke schválení nebo schválené.",
                SubmittedTimesheetCount: submitted,
                ApprovedTimesheetCount: approved);
        }

        return new ContractEmployeeAddImpact(CanAdd: true, BlockReason: null, SubmittedTimesheetCount: 0, ApprovedTimesheetCount: 0);
    }
}

