using ClosedXML.Excel;
using NSubstitute;
using Timesheets.Api.Timesheets;
using Xunit;

namespace Timesheets.Api.Tests;

public class AttendanceSpreadsheetTests
{
    [Fact]
    public void Read_ValidFile_ReturnsCorrectTimesheet()
    {
        string filePath = Path.Combine("Unit", "TestData", "valid_attendance.xlsx");
        using FileStream stream = File.OpenRead(filePath);
        AttendanceTimesheet result = AttendanceSpreadsheet.Read(stream);

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
        AttendanceTimesheet result = AttendanceSpreadsheet.Read(stream);

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
        AttendanceTimesheet result = AttendanceSpreadsheet.Read(stream);

        Assert.NotNull(result);
        Assert.Multiple(() =>
        {
            Assert.Null(result.Days[1].ClockIn);
            Assert.Empty(result.Days[1].Schedules);
        });
    }

    [Theory]
    [InlineData("8:00", 8, 0)]
    [InlineData("08:00", 8, 0)]
    [InlineData("17:30:00", 17, 30)]
    [InlineData(" 8:15 ", 8, 15)]
    public void ParseTime_ValidFormat_ReturnsTimeSpan(string input, int expectedHours, int expectedMinutes)
    {
        IXLCell cell = Substitute.For<IXLCell>();
        cell.GetString().Returns(input);
        Assert.Equal(new TimeSpan(expectedHours, expectedMinutes, 0), AttendanceSpreadsheet.ParseTime(cell));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("8.30")]
    [InlineData("invalid")]
    [InlineData("25:00")]
    public void ParseTime_InvalidFormatOrEmpty_ReturnsNull(string input)
    {
        IXLCell cell = Substitute.For<IXLCell>();
        cell.GetString().Returns(input);
        Assert.Null(AttendanceSpreadsheet.ParseTime(cell));
    }

    [Fact]
    public void ParseTimeRanges_ValidRanges_ReturnsRanges()
    {
        IXLCell cell = Substitute.For<IXLCell>();
        cell.GetString().Returns("08:00-12:00, 13:00-17:00");
        IReadOnlyList<TimeRange> result = AttendanceSpreadsheet.ParseTimeRanges(cell);
        Assert.Equal([new TimeRange(new TimeSpan(8, 0, 0), new TimeSpan(12, 0, 0)), new TimeRange(new TimeSpan(13, 0, 0), new TimeSpan(17, 0, 0))], result);
    }

    [Theory]
    [InlineData("08:00 to 12:00")]
    [InlineData("08:00-")]
    [InlineData("invalid-range")]
    public void ParseTimeRanges_InvalidFormat_ReturnsEmptyList(string input)
    {
        IXLCell cell = Substitute.For<IXLCell>();
        cell.GetString().Returns(input);
        Assert.Empty(AttendanceSpreadsheet.ParseTimeRanges(cell));
    }

    [Fact]
    public void ReadMetadata_ValidFile_ReturnsCorrectMetadata()
    {
        string filePath = Path.Combine("Unit", "TestData", "valid_attendance.xlsx");
        using FileStream stream = File.OpenRead(filePath);
        AttendanceTimesheetMetadata result = AttendanceSpreadsheet.ReadMetadata(stream);
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
    public void ReadMetadata_MalformedMetadata_HandlesGracefully()
    {
        string filePath = Path.Combine("Unit", "TestData", "invalid_attendance_malformed_metadata.xlsx");
        using FileStream stream = File.OpenRead(filePath);
        AttendanceTimesheetMetadata result = AttendanceSpreadsheet.ReadMetadata(stream);
        Assert.Multiple(() =>
        {
            Assert.Equal(string.Empty, result.EmployeePersonalNumber);
            Assert.Equal(2024, result.Year);
            Assert.Equal(10, result.Month);
            Assert.Equal(31, result.DaysInMonth);
        });
    }
}
