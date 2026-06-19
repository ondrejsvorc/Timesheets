using Timesheets.Api.Timesheets;

namespace Timesheets.Api.Tests.Unit;

public sealed class TimesheetLogicTests
{
    [Fact]
    public void Overnight_shift_is_valid()
    {
        AttendanceDay day = Day(date: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), clockIn: new TimeSpan(22, 0, 0), clockOut: new TimeSpan(6, 0, 0), breakStart: new TimeSpan(2, 0, 0), breakEnd: new TimeSpan(2, 30, 0));

        AttendanceTimesheet timesheet = new(EmployeePersonalNumber: "1", EmployeeName: "Test", Workload: 1m, Year: 2026, Month: 6, Days: [day]);
        TimesheetReview review = new AttendanceTimesheetReviewer().Review(timesheet);

        Assert.Equal(7.5m, day.TotalHours);
        Assert.DoesNotContain(review.DayIssues, issue => issue.Code is "ERR-ATT-02" or "ERR-ATT-05");
    }

    [Fact]
    public void Twelve_worked_hours_with_break_does_not_exceed_daily_limit()
    {
        AttendanceDay day = Day(date: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), clockIn: new TimeSpan(8, 0, 0), clockOut: new TimeSpan(20, 30, 0), breakStart: new TimeSpan(12, 0, 0), breakEnd: new TimeSpan(12, 30, 0));

        AttendanceTimesheet timesheet = new(EmployeePersonalNumber: "1", EmployeeName: "Test", Workload: 1m, Year: 2026, Month: 6, Days: [day]);
        TimesheetReview review = new AttendanceTimesheetReviewer().Review(timesheet);

        Assert.Equal(12m, day.TotalHours);
        Assert.DoesNotContain(review.DayIssues, issue => issue.Code == "ERR-ATT-05");
    }

    [Fact]
    public void Weekday_holiday_remains_in_hours_obligation()
    {
        AttendanceDay day = Day(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), isHoliday: true);

        Assert.Equal(8m, day.TotalHoursObligation);
    }

    [Fact]
    public void Weekend_has_no_hours_obligation()
    {
        AttendanceDay day = Day(new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc), isHoliday: true);

        Assert.Equal(0m, day.TotalHoursObligation);
    }

    [Fact]
    public void Overnight_shift_end_is_used_for_rest()
    {
        AttendanceDay previous = Day(date: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), clockIn: new TimeSpan(22, 0, 0), clockOut: new TimeSpan(2, 0, 0));
        AttendanceDay current = Day(date: new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc), clockIn: new TimeSpan(10, 0, 0), clockOut: new TimeSpan(18, 0, 0));

        AttendanceTimesheet timesheet = new(EmployeePersonalNumber: "1", EmployeeName: "Test", Workload: 1m, Year: 2026, Month: 6, Days: [previous, current]);
        TimesheetReview review = new AttendanceTimesheetReviewer().Review(timesheet);

        Assert.Contains(review.Issues, issue => issue.Code == "ERR-COM-05");
    }

    [Fact]
    public void Combined_workload_is_not_multiplied_by_days()
    {
        CombinedDay first = CombinedDay(1);
        CombinedDay second = CombinedDay(2);

        CombinedTimesheet timesheet = new(Year: 2026, Month: 6, CoreWorkload: 1m, Days: [first, second]);
        Assert.Equal(1m, timesheet.TotalWorkload);
    }

    private static AttendanceDay Day(DateTime date, TimeSpan? clockIn = null, TimeSpan? clockOut = null, TimeSpan? breakStart = null, TimeSpan? breakEnd = null, bool isHoliday = false) => new(Date: date, ClockIn: clockIn, ClockOut: clockOut, BreakStart: breakStart, BreakEnd: breakEnd, OtherInterruption: null, Schedules: [], IsHoliday: isHoliday, Workload: 1m);

    private static CombinedDay CombinedDay(int day) => new(Date: new DateTime(2026, 6, day, 0, 0, 0, DateTimeKind.Utc), IsHoliday: false, Workload: 1m, CoreWorkload: 1m, WorkedHours: 8m, CoreHours: 8m, ProjectHours: 0m, StagHours: 0m, HasAttendanceFilled: true, SkipAllocationRules: false);
}
