namespace Timesheets.Api.Domain.Models;

public sealed class TimesheetStatus
{
    public const string DraftCode = "DRAFT";
    public const string SubmittedCode = "SUBMITTED";
    public const string ApprovedCode = "APPROVED";

    public static readonly Guid DraftId = Guid.Parse("00000000-0000-0000-0000-000000000020");
    public static readonly Guid SubmittedId = Guid.Parse("00000000-0000-0000-0000-000000000021");
    public static readonly Guid ApprovedId = Guid.Parse("00000000-0000-0000-0000-000000000022");

    public const string DraftName = "Rozpracovaný";
    public const string SubmittedName = "Ke schválení";
    public const string ApprovedName = "Schválený";

    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<Timesheet> Timesheets { get; set; } = [];

    public bool IsDraft => Code == DraftCode;
    public bool IsSubmitted => Code == SubmittedCode;
    public bool IsApproved => Code == ApprovedCode;

    public static Guid StatusId(string code) => code switch
    {
        DraftCode => DraftId,
        SubmittedCode => SubmittedId,
        ApprovedCode => ApprovedId,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null),
    };

    public static bool IsValidAttendanceTransition(TimesheetStatus from, string toCode) => IsValidTransition(from.Code, toCode);
    public static bool IsValidAttendanceTransition(TimesheetStatus from, TimesheetStatus to) => IsValidTransition(from.Code, to.Code);
    public static bool IsValidAttendanceTransition(Guid fromId, Guid toId) => IsValidTransition(CodeForId(fromId), CodeForId(toId));

    public static bool IsValidProjectTransition(TimesheetStatus from, string toCode) => IsValidTransition(from.Code, toCode);
    public static bool IsValidProjectTransition(TimesheetStatus from, TimesheetStatus to) => IsValidTransition(from.Code, to.Code);
    public static bool IsValidProjectTransition(Guid fromId, Guid toId) => IsValidTransition(CodeForId(fromId), CodeForId(toId));

    public static string ResolveContractPartDisplayStatus(string statusCode) => statusCode switch
    {
        SubmittedCode => SubmittedName,
        ApprovedCode => ApprovedName,
        _ => DraftName,
    };

    public static string ResolveContractPartDisplayStatus(Guid projectStatusId) => ResolveContractPartDisplayStatus(CodeForId(projectStatusId));

    public static string CodeForId(Guid id) => id switch
    {
        Guid statusId when statusId == DraftId => DraftCode,
        Guid statusId when statusId == SubmittedId => SubmittedCode,
        Guid statusId when statusId == ApprovedId => ApprovedCode,
        _ => DraftCode,
    };

    private static bool IsValidTransition(string fromCode, string toCode) => (fromCode, toCode) switch
    {
        (DraftCode, SubmittedCode) => true,
        (SubmittedCode, ApprovedCode) => true,
        (SubmittedCode, DraftCode) => true,
        (ApprovedCode, DraftCode) => true,
        (string from, string to) when from == to => true,
        _ => false,
    };
}
