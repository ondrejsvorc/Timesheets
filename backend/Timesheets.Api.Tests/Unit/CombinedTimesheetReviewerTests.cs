using Timesheets.Api.Timesheets;
using Xunit;

namespace Timesheets.Api.Tests.Unit;

public sealed class CombinedTimesheetReviewerTests
{
    private static CombinedDay Day(int day, decimal worked, decimal core, decimal projects, decimal stag = 0, decimal coreWorkload = 1, bool skipAllocation = false) =>
        new(Date: new DateTime(2026, 6, day, 0, 0, 0, DateTimeKind.Utc), IsHoliday: false, Workload: 1, CoreWorkload: coreWorkload, WorkedHours: worked, CoreHours: core, ProjectHours: projects, StagHours: stag, HasAttendanceFilled: true, SkipAllocationRules: skipAllocation);

    private static AttendanceTimesheet EmptyAttendance(int year, int month) => new("1", "Test", 1, year, month, []);

    [Fact]
    public void Review_flags_unbalanced_day()
    {
        CombinedTimesheet combined = new(2026, 6, 1, [Day(2, worked: 8, core: 4, projects: 2)]);
        TimesheetReview review = new CombinedTimesheetReviewer().Review(combined, EmptyAttendance(2026, 6), tracksAttendance: true);
        Assert.True(review.HasErrors);
        Assert.Contains(review.DayIssues, issue => issue.Code == "ERR-ALL-01" && issue.Field == "balance");
    }

    [Fact]
    public void Review_accepts_balanced_day()
    {
        CombinedTimesheet combined = new(2026, 6, 1, [Day(2, worked: 8, core: 3, projects: 5)]);
        TimesheetReview review = new CombinedTimesheetReviewer().Review(combined, EmptyAttendance(2026, 6), tracksAttendance: true);
        Assert.DoesNotContain(review.DayIssues, issue => issue.Code == "ERR-ALL-01");
    }

    [Fact]
    public void Review_requires_core_at_least_stag_when_kmen_workload_exists()
    {
        CombinedTimesheet combined = new(2026, 6, 0.5m, [Day(2, worked: 8, core: 1, projects: 7, stag: 2)]);
        TimesheetReview review = new CombinedTimesheetReviewer().Review(combined, EmptyAttendance(2026, 6), tracksAttendance: true);
        Assert.Contains(review.DayIssues, issue => issue.Code == "ERR-ALL-02");
    }

    [Fact]
    public void Review_skips_stag_rule_for_business_trip()
    {
        CombinedTimesheet combined = new(2026, 6, 1, [Day(2, worked: 8, core: 0, projects: 0, stag: 2, skipAllocation: true)]);
        TimesheetReview review = new CombinedTimesheetReviewer().Review(combined, EmptyAttendance(2026, 6), tracksAttendance: true);
        Assert.DoesNotContain(review.DayIssues, issue => issue.Code is "ERR-ALL-01" or "ERR-ALL-02");
    }

    [Fact]
    public void Review_does_not_treat_weekly_hours_as_overtime()
    {
        CombinedTimesheet combined = new(2026, 6, 1, [Day(1, 10, 10, 0), Day(2, 10, 10, 0), Day(3, 10, 10, 0), Day(4, 10, 10, 0), Day(5, 10, 10, 0)]);
        TimesheetReview review = new CombinedTimesheetReviewer().Review(combined, EmptyAttendance(2026, 6), tracksAttendance: true);

        Assert.DoesNotContain(review.Issues, issue => issue.Code == "ERR-COM-04");
    }
}
