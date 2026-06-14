using ClosedXML.Excel;
using Timesheets.Api.Timesheets;
using Xunit;

namespace Timesheets.Api.Tests;

public class AttendanceTimesheetReaderTests
{
    private readonly ICellParser _parser = new CellParser();
    private readonly AttendanceTimesheetReader _reader;

    public AttendanceTimesheetReaderTests() => _reader = new AttendanceTimesheetReader(_parser);

    [Fact]
    public void Read_ValidFile_ReturnsCorrectTimesheet()
    {
        string filePath = Path.Combine("Unit", "TestData", "valid_attendance.xlsx");
        using FileStream stream = File.OpenRead(filePath);
        AttendanceTimesheet result = _reader.Read(stream);

        Assert.NotNull(result);
        Assert.Multiple(() =>
        {
            Assert.False(string.IsNullOrWhiteSpace(result.EmployeePersonalNumber));
            Assert.False(string.IsNullOrWhiteSpace(result.EmployeeName));
            Assert.Equal(2024, result.Year);
            Assert.Equal(10, result.Month);
            Assert.Equal(31, result.Days.Count);
        });

        AttendanceDay firstDay = result.Days[0];
        Assert.Equal(new DateTime(2024, 10, 1), firstDay.Date);
    }

    [Fact]
    public void Read_MalformedMetadata_HandlesGracefully()
    {
        string filePath = Path.Combine("Unit", "TestData", "invalid_attendance_malformed_metadata.xlsx");
        using FileStream stream = File.OpenRead(filePath);
        AttendanceTimesheet result = _reader.Read(stream);

        Assert.NotNull(result);
        Assert.Multiple(() =>
        {
            Assert.Equal(string.Empty, result.EmployeePersonalNumber);
            Assert.Equal(2024, result.Year);
            Assert.Equal(10, result.Month);
        });
    }

    [Fact]
    public void Read_MalformedTimes_ReturnsNullForInvalidCells()
    {
        string filePath = Path.Combine("Unit", "TestData", "invalid_attendance_malformed_times.xlsx");
        using FileStream stream = File.OpenRead(filePath);
        AttendanceTimesheet result = _reader.Read(stream);

        Assert.NotNull(result);
        Assert.Multiple(() =>
        {
            Assert.Null(result.Days[1].ClockIn);
            Assert.Empty(result.Days[1].Schedules);
        });
    }
}
