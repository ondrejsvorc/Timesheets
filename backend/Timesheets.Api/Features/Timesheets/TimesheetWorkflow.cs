using Timesheets.Api.Domain.Models;

namespace Timesheets.Api.Features.Timesheets;

internal static class TimesheetWorkflow
{
    public static readonly Guid DraftStatusId = Guid.Parse("00000000-0000-0000-0000-000000000020");
    public static readonly Guid SubmittedStatusId = Guid.Parse("00000000-0000-0000-0000-000000000021");
    public static readonly Guid ApprovedStatusId = Guid.Parse("00000000-0000-0000-0000-000000000022");

    public const string DraftStatusName = "Rozpracovaný";
    public const string SubmittedStatusName = "Ke schválení";
    public const string ApprovedStatusName = "Schválený";

    public static Guid StatusId(string code) => code switch
    {
        TimesheetStatusCodes.Draft => DraftStatusId,
        TimesheetStatusCodes.Submitted => SubmittedStatusId,
        TimesheetStatusCodes.Approved => ApprovedStatusId,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null),
    };

    public static bool IsDraft(TimesheetStatus status) => status.Code == TimesheetStatusCodes.Draft;
    public static bool IsSubmitted(TimesheetStatus status) => status.Code == TimesheetStatusCodes.Submitted;
    public static bool IsApproved(TimesheetStatus status) => status.Code == TimesheetStatusCodes.Approved;

    public static bool IsDraft(string code) => code == TimesheetStatusCodes.Draft;
    public static bool IsSubmitted(string code) => code == TimesheetStatusCodes.Submitted;
    public static bool IsApproved(string code) => code == TimesheetStatusCodes.Approved;

    public static bool IsValidAttendanceTransition(TimesheetStatus from, string toCode) => IsValidTransition(from.Code, toCode);
    public static bool IsValidAttendanceTransition(TimesheetStatus from, TimesheetStatus to) => IsValidTransition(from.Code, to.Code);
    public static bool IsValidAttendanceTransition(Guid fromId, Guid toId) => IsValidTransition(CodeForId(fromId), CodeForId(toId));

    public static bool IsValidProjectTransition(TimesheetStatus from, string toCode) => IsValidTransition(from.Code, toCode);
    public static bool IsValidProjectTransition(TimesheetStatus from, TimesheetStatus to) => IsValidTransition(from.Code, to.Code);
    public static bool IsValidProjectTransition(Guid fromId, Guid toId) => IsValidTransition(CodeForId(fromId), CodeForId(toId));

    public static string ResolveContractPartDisplayStatus(string statusCode) => statusCode switch
    {
        TimesheetStatusCodes.Submitted => SubmittedStatusName,
        TimesheetStatusCodes.Approved => ApprovedStatusName,
        _ => DraftStatusName,
    };

    public static string ResolveContractPartDisplayStatus(Guid projectStatusId) => ResolveContractPartDisplayStatus(CodeForId(projectStatusId));

    private static bool IsValidTransition(string fromCode, string toCode) => (fromCode, toCode) switch
    {
        (TimesheetStatusCodes.Draft, TimesheetStatusCodes.Submitted) => true,
        (TimesheetStatusCodes.Submitted, TimesheetStatusCodes.Approved) => true,
        (TimesheetStatusCodes.Submitted, TimesheetStatusCodes.Draft) => true,
        (TimesheetStatusCodes.Approved, TimesheetStatusCodes.Draft) => true,
        (string from, string to) when from == to => true,
        _ => false,
    };

    private static string CodeForId(Guid id) => id switch
    {
        Guid statusId when statusId == DraftStatusId => TimesheetStatusCodes.Draft,
        Guid statusId when statusId == SubmittedStatusId => TimesheetStatusCodes.Submitted,
        Guid statusId when statusId == ApprovedStatusId => TimesheetStatusCodes.Approved,
        _ => TimesheetStatusCodes.Draft,
    };
}
