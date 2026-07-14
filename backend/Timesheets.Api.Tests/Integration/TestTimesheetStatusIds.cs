namespace Timesheets.Api.Tests.Integration;

internal static class TestTimesheetStatusIds
{
    public static readonly Guid Draft = Guid.Parse("00000000-0000-0000-0000-000000000020");
    public static readonly Guid Submitted = Guid.Parse("00000000-0000-0000-0000-000000000021");
    public static readonly Guid Approved = Guid.Parse("00000000-0000-0000-0000-000000000022");

    public const string DraftCode = "DRAFT";
    public const string SubmittedCode = "SUBMITTED";
    public const string ApprovedCode = "APPROVED";
}
