using Timesheets.Api.Timesheets;

namespace Timesheets.Api.Tests.Unit;

public sealed class TimesheetLogicTests
{
    [Fact]
    public void Overnight_shift_is_valid()
    {
        AttendanceDay day = Day(
            date: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            clockIn: new TimeSpan(22, 0, 0),
            clockOut: new TimeSpan(6, 0, 0),
            breakStart: new TimeSpan(2, 0, 0),
            breakEnd: new TimeSpan(2, 30, 0));

        TimesheetReview review = new AttendanceTimesheetReviewer().Review(new AttendanceTimesheet("1", "Test", 1m, 2026, 6, [day]));

        Assert.Equal(7.5m, day.TotalHours);
        Assert.DoesNotContain(review.DayIssues, issue => issue.Code is "ERR-ATT-02" or "ERR-ATT-05");
    }

    [Fact]
    public void Holiday_has_no_hours_obligation()
    {
        AttendanceDay day = Day(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), isHoliday: true);

        Assert.Equal(0m, day.TotalHoursObligation);
    }

    [Fact]
    public void Overnight_shift_end_is_used_for_rest()
    {
        AttendanceDay previous = Day(
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            new TimeSpan(22, 0, 0),
            new TimeSpan(2, 0, 0));
        AttendanceDay current = Day(
            new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc),
            new TimeSpan(10, 0, 0),
            new TimeSpan(18, 0, 0));

        TimesheetReview review = new AttendanceTimesheetReviewer().Review(new AttendanceTimesheet("1", "Test", 1m, 2026, 6, [previous, current]));

        Assert.Contains(review.Issues, issue => issue.Code == "ERR-COM-05");
    }

    [Fact]
    public void Combined_workload_is_not_multiplied_by_days()
    {
        CombinedDay first = CombinedDay(1);
        CombinedDay second = CombinedDay(2);

        Assert.Equal(1m, new CombinedTimesheet(2026, 6, 1m, [first, second]).TotalWorkload);
    }

    private static AttendanceDay Day(
        DateTime date,
        TimeSpan? clockIn = null,
        TimeSpan? clockOut = null,
        TimeSpan? breakStart = null,
        TimeSpan? breakEnd = null,
        bool isHoliday = false) =>
        new(date, clockIn, clockOut, breakStart, breakEnd, null, [], isHoliday, 1m);

    private static CombinedDay CombinedDay(int day) =>
        new(new DateTime(2026, 6, day, 0, 0, 0, DateTimeKind.Utc), false, 1m, 1m, 8m, 8m, 0m, 0m, true, false);
}
