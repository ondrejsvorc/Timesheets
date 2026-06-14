using Timesheets.Api.Timesheets;
using Xunit;

namespace Timesheets.Api.Tests;

public class AttendanceTimesheetMetadataReaderTests
{
    private readonly AttendanceTimesheetMetadataReader _reader = new();

    [Fact]
    public void Read_ValidFile_ReturnsCorrectMetadata()
    {
        string filePath = Path.Combine("Unit", "TestData", "valid_attendance.xlsx");
        using FileStream stream = File.OpenRead(filePath);
        AttendanceTimesheetMetadata result = _reader.Read(stream);
        Assert.Multiple(() =>
        {
            Assert.False(string.IsNullOrWhiteSpace(result.EmployeePersonalNumber));
            Assert.False(string.IsNullOrWhiteSpace(result.EmployeeName));
            Assert.Equal(2024, result.Year);
            Assert.Equal(10, result.Month);
            Assert.Equal(31, result.DaysInMonth);
        });
    }

    [Fact]
    public void Read_MalformedMetadata_HandlesGracefully()
    {
        string filePath = Path.Combine("Unit", "TestData", "invalid_attendance_malformed_metadata.xlsx");
        using FileStream stream = File.OpenRead(filePath);
        AttendanceTimesheetMetadata result = _reader.Read(stream);
        Assert.Multiple(() =>
        {
            Assert.Equal(string.Empty, result.EmployeePersonalNumber);
            Assert.Equal(2024, result.Year);
            Assert.Equal(10, result.Month);
            Assert.Equal(31, result.DaysInMonth);
        });
    }
}
