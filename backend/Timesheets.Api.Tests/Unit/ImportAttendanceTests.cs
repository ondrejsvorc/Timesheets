using Timesheets.Api.Features.Attendance.Endpoints;

namespace Timesheets.Api.Tests.Unit;

public sealed class ImportAttendanceTests
{
    [Fact]
    public void NormalizeInterruptions_PreservesHalfDayMarker()
    {
        HashSet<string> validCodes = new(StringComparer.OrdinalIgnoreCase) { "D", "JMV/HO", "ZV" };

        string? result = ImportAttendance.NormalizeInterruptions("D p\u016flden (0),JMV/ p\u016flden (0)", validCodes);

        Assert.Equal("D p\u016flden,JMV/HO p\u016flden", result);
    }
}
