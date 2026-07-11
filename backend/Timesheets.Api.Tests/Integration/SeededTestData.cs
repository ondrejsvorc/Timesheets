namespace Timesheets.Api.Tests.Integration;

internal static class SeededTestData
{
    public static readonly Guid AlphaProjectId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    public static readonly Guid BetaProjectId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    public static readonly Guid AlphaContractId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    public static readonly Guid BetaContractId = Guid.Parse("30000000-0000-0000-0000-000000000002");
    public static readonly Guid JanNovakEmployeeId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid MarieEmployeeId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid PetrEmployeeId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    public static readonly Guid MarieVpPositionId = Guid.Parse("60000000-0000-0000-0000-000000000001");
    public static readonly Guid PetrTechnikPositionId = Guid.Parse("60000000-0000-0000-0000-000000000002");
    public static readonly Guid MarieDecContractPartId = Guid.Parse("90000000-0000-0000-0000-000000000002");
    public static readonly Guid PetrDecTimesheetId = Guid.Parse("70000000-0000-0000-0000-000000000003");
    public static readonly Guid PetrDecContractPartId = Guid.Parse("90000000-0000-0000-0000-000000000003");
}
