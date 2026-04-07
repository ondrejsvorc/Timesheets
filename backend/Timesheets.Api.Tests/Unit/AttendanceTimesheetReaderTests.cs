using ClosedXML.Excel;
using Timesheets.Api.Timesheets;
using Xunit;

namespace Timesheets.Api.Tests;

public class AttendanceTimesheetReaderTests
{
    private readonly ICellParser _parser = new CellParser();
    private readonly AttendanceTimesheetReader _reader;

    public AttendanceTimesheetReaderTests()
    {
        _reader = new AttendanceTimesheetReader(_parser);
    }

    [Fact]
    public void Read_ValidFile_ReturnsCorrectTimesheet()
    {
        // Arrange
        string filePath = Path.Combine("Unit", "TestData", "valid_attendance.xlsx");
        using FileStream stream = File.OpenRead(filePath);

        // Act
        AttendanceTimesheet result = _reader.Read(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Multiple(() =>
        {
            Assert.True(result.EmployeePersonalNumber > 0);
            Assert.False(string.IsNullOrWhiteSpace(result.EmployeeName));
            Assert.Equal(2024, result.Year);
            Assert.Equal(10, result.Month);
            Assert.Equal(31, result.Days.Count);
        });

        // Check a specific day (e.g., Oct 1st)
        // Note: headerOffset is 3, so Oct 1st is row 4.
        AttendanceDay firstDay = result.Days[0];
        Assert.Equal(new DateTime(2024, 10, 1), firstDay.Date);
    }

    [Fact]
    public void Read_MalformedMetadata_HandlesGracefully()
    {
        // Arrange
        string filePath = Path.Combine("Unit", "TestData", "invalid_attendance_malformed_metadata.xlsx");
        using FileStream stream = File.OpenRead(filePath);

        // Act
        AttendanceTimesheet result = _reader.Read(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Multiple(() =>
        {
            // In this file, A1 is missing the number, so personal number should be 0
            Assert.Equal(0, result.EmployeePersonalNumber);

            // But A2 still contains "01.10.2024", so Year/Month are actually parsed!
            Assert.Equal(2024, result.Year);
            Assert.Equal(10, result.Month);
        });
    }

    [Fact]
    public void Read_MalformedTimes_ReturnsNullForInvalidCells()
    {
        // Arrange
        string filePath = Path.Combine("Unit", "TestData", "invalid_attendance_malformed_times.xlsx");
        using FileStream stream = File.OpenRead(filePath);

        // Act
        AttendanceTimesheet result = _reader.Read(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Multiple(() =>
        {
            // Row 5 (index 1) has malformed time "8.30" in B5 (ClockIn)
            Assert.Null(result.Days[1].ClockIn);
            // Row 5 has malformed range "08:00 to 12:00" in K5 (Schedules)
            Assert.Empty(result.Days[1].Schedules);
        });
    }

    [Fact]
    public void Read_ShortMonth_ReturnsCorrectNumberOfDays()
    {
        // Arrange
        string filePath = Path.Combine("Unit", "TestData", "edge_case_attendance_short_month.xlsx");
        using FileStream stream = File.OpenRead(filePath);

        // Act
        AttendanceTimesheet result = _reader.Read(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Multiple(() =>
        {
            // Note: The file has 01.10.2024 as start, so it reads 10 (October)
            Assert.Equal(10, result.Month);
            Assert.Equal(31, result.Days.Count);
        });
    }
}
