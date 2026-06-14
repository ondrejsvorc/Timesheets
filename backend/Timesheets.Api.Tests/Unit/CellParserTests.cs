using ClosedXML.Excel;
using NSubstitute;
using Timesheets.Api.Timesheets;
using Xunit;

namespace Timesheets.Api.Tests;

public class CellParserTests
{
    private readonly CellParser _sut = new();

    [Theory]
    [InlineData("8:00", 8, 0)]
    [InlineData("08:00", 8, 0)]
    [InlineData("17:30:00", 17, 30)]
    [InlineData(" 8:15 ", 8, 15)]
    public void ParseTime_ValidFormat_ReturnsTimeSpan(string input, int expectedHours, int expectedMinutes)
    {
        IXLCell cell = Substitute.For<IXLCell>();
        cell.GetString().Returns(input);
        TimeSpan? result = _sut.ParseTime(cell);
        Assert.Equal(new TimeSpan(expectedHours, expectedMinutes, 0), result);
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
        TimeSpan? result = _sut.ParseTime(cell);
        Assert.Null(result);
    }

    [Theory]
    [InlineData("8.5", 8.5)]
    [InlineData("8,5", 8.5)]
    [InlineData("10", 10.0)]
    [InlineData(" 100.00 ", 100.0)]
    public void ParseDecimal_ValidFormat_ReturnsDecimal(string input, decimal expected)
    {
        IXLCell cell = Substitute.For<IXLCell>();
        cell.GetString().Returns(input);
        decimal? result = _sut.ParseDecimal(cell);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    public void ParseDecimal_InvalidFormat_ReturnsNull(string input)
    {
        IXLCell cell = Substitute.For<IXLCell>();
        cell.GetString().Returns(input);
        decimal? result = _sut.ParseDecimal(cell);
        Assert.Null(result);
    }

    [Fact]
    public void ParseTimeRanges_ValidRanges_ReturnsRanges()
    {
        IXLCell cell = Substitute.For<IXLCell>();
        cell.GetString().Returns("08:00-12:00, 13:00-17:00");
        IReadOnlyList<TimeRange> result = _sut.ParseTimeRanges(cell);
        Assert.Equal(2, result.Count);
        Assert.Equal(new TimeSpan(8, 0, 0), result[0].Start);
        Assert.Equal(new TimeSpan(12, 0, 0), result[0].End);
        Assert.Equal(new TimeSpan(13, 0, 0), result[1].Start);
        Assert.Equal(new TimeSpan(17, 0, 0), result[1].End);
    }

    [Theory]
    [InlineData("08:00 to 12:00")]
    [InlineData("08:00-")]
    [InlineData("invalid-range")]
    public void ParseTimeRanges_InvalidFormat_ReturnsEmptyList(string input)
    {
        IXLCell cell = Substitute.For<IXLCell>();
        cell.GetString().Returns(input);
        IReadOnlyList<TimeRange> result = _sut.ParseTimeRanges(cell);
        Assert.Empty(result);
    }
}
