using Timesheets.Api.Domain.Models;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Timesheets;

public sealed record TimesheetMonthActions(bool Edit, bool Save, bool Submit, bool FinalApprove, bool ReturnWhole, bool Unlock);

public sealed record TimesheetContractPartActions(bool ApproveProject, bool ReturnProject);

internal static class TimesheetActions
{
    internal static TimesheetMonthActions ResolveMonthActions(ICurrentUser user, Guid employeeId, string statusCode, bool allContractPartsApproved)
    {
        bool canManage = user.IsGlobalManagerRole() || user.EmployeeId == employeeId;

        return statusCode switch
        {
            TimesheetStatus.DraftCode => new TimesheetMonthActions(
                Edit: canManage,
                Save: canManage,
                Submit: canManage,
                FinalApprove: false,
                ReturnWhole: false,
                Unlock: false),
            TimesheetStatus.SubmittedCode => new TimesheetMonthActions(
                Edit: false,
                Save: false,
                Submit: false,
                FinalApprove: canManage && allContractPartsApproved,
                ReturnWhole: canManage,
                Unlock: false),
            TimesheetStatus.ApprovedCode => new TimesheetMonthActions(
                Edit: false,
                Save: false,
                Submit: false,
                FinalApprove: false,
                ReturnWhole: false,
                Unlock: canManage),
            _ => new TimesheetMonthActions(false, false, false, false, false, false),
        };
    }

    internal static TimesheetContractPartActions ResolveContractPartActions(
        ICurrentUser user,
        string monthStatusCode,
        string partStatusCode,
        Guid? contractId,
        Guid? projectId)
    {
        if (monthStatusCode != TimesheetStatus.SubmittedCode || contractId is null || projectId is null)
        {
            return new TimesheetContractPartActions(false, false);
        }

        if (!user.CanManageContract(contractId.Value, projectId.Value))
        {
            return new TimesheetContractPartActions(false, false);
        }

        return new TimesheetContractPartActions(
            ApproveProject: partStatusCode == TimesheetStatus.SubmittedCode,
            ReturnProject: partStatusCode == TimesheetStatus.ApprovedCode);
    }
}
