using Timesheets.Api.Features.Timesheets;
using Xunit;

namespace Timesheets.Api.Tests.Unit;

public sealed class EvaluatedTimesheetReviewerTests
{
    private static EvaluatedDay Day(int day, decimal worked, decimal core, decimal projects, decimal stag = 0, decimal coreWorkload = 1, bool skipAllocation = false) =>
        new(Date: new DateTime(2026, 6, day, 0, 0, 0, DateTimeKind.Utc), IsHoliday: false, Workload: 1, CoreWorkload: coreWorkload, WorkedHours: worked, CoreHours: core, ContractPartHours: projects, StagHours: stag, HasAttendanceFilled: true, SkipAllocationRules: skipAllocation);

    private static EvaluatedDay DayWithoutAttendance(int day, decimal core, decimal projects, decimal stag = 0) =>
        new(Date: new DateTime(2026, 6, day, 0, 0, 0, DateTimeKind.Utc), IsHoliday: false, Workload: 1, CoreWorkload: 1, WorkedHours: 0, CoreHours: core, ContractPartHours: projects, StagHours: stag, HasAttendanceFilled: false, SkipAllocationRules: false);

    private static AttendanceTimesheet EmptyAttendance(int year, int month) => new("1", "Test", 1, year, month, []);

    [Fact]
    public void Review_flags_unbalanced_day_when_allocation_is_short()
    {
        EvaluatedTimesheet evaluated = new(2026, 6, 1, [Day(2, worked: 8, core: 4, projects: 2)]);
        TimesheetReview review = new EvaluatedTimesheetReviewer().Review(evaluated, EmptyAttendance(2026, 6), tracksAttendance: true);
        Assert.True(review.HasErrors);
        Assert.Contains(review.DayIssues, issue => issue.Code == "ERR-ALL-01" && issue.Field == "balance");
    }

    [Fact]
    public void Review_warns_when_allocation_exceeds_attendance()
    {
        EvaluatedTimesheet evaluated = new(2026, 6, 1, [Day(2, worked: 8, core: 10, projects: 0)]);
        TimesheetReview review = new EvaluatedTimesheetReviewer().Review(evaluated, EmptyAttendance(2026, 6), tracksAttendance: true);
        Assert.Contains(review.DayIssues, issue => issue.Code == "WAR-ALL-05" && issue.Field == "balance");
        Assert.DoesNotContain(review.DayIssues, issue => issue.Type == IssueType.Error);
    }

    [Fact]
    public void Review_accepts_balanced_day()
    {
        EvaluatedTimesheet evaluated = new(2026, 6, 1, [Day(2, worked: 8, core: 3, projects: 5)]);
        TimesheetReview review = new EvaluatedTimesheetReviewer().Review(evaluated, EmptyAttendance(2026, 6), tracksAttendance: true);
        Assert.DoesNotContain(review.DayIssues, issue => issue.Code == "ERR-ALL-01");
    }

    [Fact]
    public void Review_requires_core_at_least_stag_for_academic_employee()
    {
        EvaluatedTimesheet evaluated = new(2026, 6, 0.5m, [Day(2, worked: 8, core: 1, projects: 7, stag: 2)]);
        TimesheetReview review = new EvaluatedTimesheetReviewer().Review(evaluated, EmptyAttendance(2026, 6), tracksAttendance: false);
        Assert.Contains(review.DayIssues, issue => issue.Code == "ERR-ALL-02");
    }

    [Fact]
    public void Review_skips_stag_rule_for_non_academic_employee()
    {
        EvaluatedTimesheet evaluated = new(2026, 6, 0.5m, [Day(2, worked: 8, core: 1, projects: 7, stag: 2)]);
        TimesheetReview review = new EvaluatedTimesheetReviewer().Review(evaluated, EmptyAttendance(2026, 6), tracksAttendance: true);

        Assert.DoesNotContain(review.DayIssues, issue => issue.Code == "ERR-ALL-02");
    }

    [Fact]
    public void Review_skips_stag_rule_for_business_trip()
    {
        EvaluatedTimesheet evaluated = new(2026, 6, 1, [Day(2, worked: 8, core: 0, projects: 0, stag: 2, skipAllocation: true)]);
        TimesheetReview review = new EvaluatedTimesheetReviewer().Review(evaluated, EmptyAttendance(2026, 6), tracksAttendance: true);
        Assert.DoesNotContain(review.DayIssues, issue => issue.Code is "ERR-ALL-01" or "ERR-ALL-02");
    }

    [Fact]
    public void Review_does_not_treat_weekly_hours_as_overtime()
    {
        EvaluatedTimesheet evaluated = new(2026, 6, 1, [Day(1, 10, 10, 0), Day(2, 10, 10, 0), Day(3, 10, 10, 0), Day(4, 10, 10, 0), Day(5, 10, 10, 0)]);
        TimesheetReview review = new EvaluatedTimesheetReviewer().Review(evaluated, EmptyAttendance(2026, 6), tracksAttendance: true);

        Assert.DoesNotContain(review.Issues, issue => issue.Code == "ERR-COM-04");
    }

    [Fact]
    public void Review_requires_attendance_when_stag_or_allocation_is_filled()
    {
        EvaluatedTimesheet evaluated = new(2026, 6, 1, [DayWithoutAttendance(2, core: 3.83m, projects: 0m, stag: 3.83m)]);
        TimesheetReview review = new EvaluatedTimesheetReviewer().Review(evaluated, EmptyAttendance(2026, 6), tracksAttendance: true);

        Assert.Contains(review.DayIssues, issue => issue.Code == "ERR-ATT-13" && issue.Field == "clockIn");
        Assert.Contains(review.DayIssues, issue => issue.Code == "ERR-ATT-13" && issue.Field == "clockOut");
        Assert.Contains(review.DayIssues, issue => issue.Code == "ERR-ATT-13" && issue.Field == "breakStart");
        Assert.Contains(review.DayIssues, issue => issue.Code == "ERR-ATT-13" && issue.Field == "breakEnd");
    }

    [Fact]
    public void Review_does_not_require_attendance_for_hidden_stag_only()
    {
        EvaluatedTimesheet evaluated = new(2026, 6, 1, [DayWithoutAttendance(2, core: 0m, projects: 0m, stag: 3.83m)]);
        TimesheetReview review = new EvaluatedTimesheetReviewer().Review(evaluated, EmptyAttendance(2026, 6), tracksAttendance: true);

        Assert.DoesNotContain(review.DayIssues, issue => issue.Code == "ERR-ATT-13");
    }

    [Fact]
    public void Review_warns_when_worked_hours_are_below_6h_for_attendance_employee()
    {
        EvaluatedTimesheet evaluated = new(2026, 6, 1, [Day(2, worked: 4, core: 4, projects: 0)]);
        TimesheetReview review = new EvaluatedTimesheetReviewer().Review(evaluated, EmptyAttendance(2026, 6), tracksAttendance: true);

        Assert.Contains(review.DayIssues, issue => issue.Code == "WAR-ALL-04" && issue.Field == "clockIn");
        Assert.Contains(review.DayIssues, issue => issue.Code == "WAR-ALL-04" && issue.Field == "clockOut");
    }

    [Fact]
    public void Review_warns_when_allocated_hours_are_below_6h_for_academic_employee()
    {
        EvaluatedTimesheet evaluated = new(2026, 6, 1, [DayWithoutAttendance(2, core: 2, projects: 2, stag: 0)]);
        TimesheetReview review = new EvaluatedTimesheetReviewer().Review(evaluated, EmptyAttendance(2026, 6), tracksAttendance: false);

        Assert.Contains(review.DayIssues, issue => issue.Code == "WAR-ALL-04" && issue.Field == "allocatedHours");
    }
}
