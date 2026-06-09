namespace Timesheets.Api.Timesheets;

internal static class TimesheetWorkflowConstants
{
    public static readonly Guid DraftStatusId = Guid.Parse("00000000-0000-0000-0000-000000000020");
    public static readonly Guid SubmittedStatusId = Guid.Parse("00000000-0000-0000-0000-000000000021");
    public static readonly Guid ApprovedStatusId = Guid.Parse("00000000-0000-0000-0000-000000000022");

    public const string DraftStatusName = "Rozpracovaný";
    public const string SubmittedStatusName = "Ke schválení";
    public const string ApprovedStatusName = "Schválený";

    public static bool IsValidAttendanceTransition(Guid from, Guid to) => (from, to) switch
    {
        (Guid f, Guid t) when f == DraftStatusId && t == SubmittedStatusId => true,
        (Guid f, Guid t) when f == SubmittedStatusId && t == ApprovedStatusId => true,
        (Guid f, Guid t) when f == SubmittedStatusId && t == DraftStatusId => true,
        (Guid f, Guid t) when f == ApprovedStatusId && t == DraftStatusId => true,
        (Guid f, Guid t) when f == t => true,
        _ => false
    };

    public static bool IsValidProjectTransition(Guid from, Guid to) => (from, to) switch
    {
        (Guid f, Guid t) when f == DraftStatusId && t == ApprovedStatusId => true,
        (Guid f, Guid t) when f == ApprovedStatusId && t == DraftStatusId => true,
        (Guid f, Guid t) when f == t => true,
        _ => false
    };

    public static string ResolveProjectDisplayStatus(Guid projectStatusId, string attendanceStatusName)
    {
        if (projectStatusId == ApprovedStatusId)
        {
            return ApprovedStatusName;
        }

        if (attendanceStatusName == SubmittedStatusName && projectStatusId == DraftStatusId)
        {
            return SubmittedStatusName;
        }

        return DraftStatusName;
    }
}
