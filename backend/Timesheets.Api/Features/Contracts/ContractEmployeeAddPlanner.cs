using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;
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

        List<string> statusCodes = await dbContext.ContractParts
            .AsNoTracking()
            .Where(t => t.ContractEmployee.ContractId == contractId && t.ContractEmployee.EmployeeId == request.EmployeeId)
            .Where(t => (t.Timesheet.Year * 100 + t.Timesheet.Month) >= startKey)
            .Where(t => endKey == null || (t.Timesheet.Year * 100 + t.Timesheet.Month) <= endKey.Value)
            .Where(t => t.TimesheetStatus.Code == TimesheetStatusCodes.Submitted || t.TimesheetStatus.Code == TimesheetStatusCodes.Approved)
            .Select(t => t.TimesheetStatus.Code)
            .ToListAsync(cancellationToken);

        int submitted = statusCodes.Count(TimesheetWorkflow.IsSubmitted);
        int approved = statusCodes.Count(TimesheetWorkflow.IsApproved);

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

