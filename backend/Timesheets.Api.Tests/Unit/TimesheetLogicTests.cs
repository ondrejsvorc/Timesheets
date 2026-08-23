using Timesheets.Api.Features.Timesheets;

namespace Timesheets.Api.Tests.Unit;

public sealed class TimesheetEvaluatorTests
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
    public void Half_day_interruption_adds_four_hours_to_total()
    {
        AttendanceDay day = Day(date: new DateTime(2026, 3, 17, 0, 0, 0, DateTimeKind.Utc), clockIn: new TimeSpan(7, 38, 0), clockOut: new TimeSpan(12, 29, 0), otherInterruption: "ZV p\u016flden");

        Assert.Equal(8.85m, day.TotalHours);
    }

    [Fact]
    public void Two_half_day_interruptions_count_as_full_day_without_attendance_times()
    {
        DateTime date = new(2026, 1, 30, 0, 0, 0, DateTimeKind.Utc);
        string interruption = "D p\u016flden,JMV/HO p\u016flden";
        AttendanceDay day = Day(date, otherInterruption: interruption);

        Assert.Equal(8m, day.TotalHours);
        Assert.Equal(8m, TimesheetEvaluator.DayCapacity(date, null, null, null, null, interruption, totalWorkload: 1m, tracksAttendance: true));
    }

    [Fact]
    public void Full_day_interruption_counts_full_obligation_without_attendance_times()
    {
        DateTime date = new(2026, 1, 29, 0, 0, 0, DateTimeKind.Utc);
        AttendanceDay day = Day(date, otherInterruption: "D");

        Assert.Equal(8m, day.TotalHours);
        Assert.Equal(8m, TimesheetEvaluator.DayCapacity(date, null, null, null, null, "D", totalWorkload: 1m, tracksAttendance: true));
        Assert.True(TimesheetEvaluator.SkipAllocationRules("D"));
    }

    [Fact]
    public void Full_day_interruption_replaces_attendance_times()
    {
        AttendanceDay day = Day(
            date: new DateTime(2026, 1, 29, 0, 0, 0, DateTimeKind.Utc),
            clockIn: new TimeSpan(8, 0, 0),
            clockOut: new TimeSpan(16, 0, 0),
            otherInterruption: "D");

        Assert.Equal(8m, day.TotalHours);
    }

    [Fact]
    public void Half_day_interruption_does_not_skip_balance_rules()
    {
        Assert.True(TimesheetEvaluator.HasProportionalInterruption("ZV p\u016flden"));
        Assert.False(TimesheetEvaluator.HasFullDayInterruption("ZV p\u016flden"));
        Assert.True(TimesheetEvaluator.HasEditableHalfDayInterruption("ZV p\u016flden"));
        Assert.False(TimesheetEvaluator.SkipAllocationRules("ZV p\u016flden"));
    }

    [Fact]
    public void Half_day_interruption_sets_minimums_without_replacing_existing_hours()
    {
        DateTime date = new(2026, 3, 17, 0, 0, 0, DateTimeKind.Utc);
        Guid firstProject = Guid.CreateVersion7();
        Guid secondProject = Guid.CreateVersion7();
        EditableTimesheetDay day = EditableDay(
            date,
            "ZV p\u016flden",
            coreHours: 3m,
            projectHours: new Dictionary<Guid, decimal> { [firstProject] = 0m, [secondProject] = 2m },
            projectLocks: new Dictionary<Guid, bool> { [firstProject] = true, [secondProject] = true });
        ContractPartColumn[] projects =
        [
            Project(firstProject, 0.25m),
            Project(secondProject, 0.25m)
        ];

        TimesheetEvaluator.ApplyInterruptionToDayState(day, projects, totalWorkload: 1m, tracksAttendance: true);

        Assert.Equal(3m, day.CoreHours);
        Assert.Equal(1m, day.ContractPartHours[firstProject]);
        Assert.Equal(2m, day.ContractPartHours[secondProject]);
        Assert.False(day.ContractPartHoursFixed[firstProject]);
        Assert.False(day.ContractPartHoursFixed[secondProject]);
    }

    [Fact]
    public void Full_day_interruption_distributes_full_obligation_without_attendance_times()
    {
        DateTime date = new(2026, 1, 29, 0, 0, 0, DateTimeKind.Utc);
        Guid firstProject = Guid.CreateVersion7();
        Guid secondProject = Guid.CreateVersion7();
        EditableTimesheetDay day = EditableDay(
            date,
            "D",
            projectHours: new Dictionary<Guid, decimal> { [firstProject] = 0m, [secondProject] = 0m });
        ContractPartColumn[] projects =
        [
            Project(firstProject, 0.25m),
            Project(secondProject, 0.25m)
        ];

        TimesheetEvaluator.ApplyInterruptionToDayState(day, projects, totalWorkload: 1m, tracksAttendance: true);

        Assert.Equal(4m, day.CoreHours);
        Assert.Equal(2m, day.ContractPartHours[firstProject]);
        Assert.Equal(2m, day.ContractPartHours[secondProject]);
    }

    [Fact]
    public void Weekday_holiday_has_no_hours_obligation()
    {
        AttendanceDay day = Day(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), isHoliday: true);

        Assert.Equal(0m, day.TotalHoursObligation);
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
    public void Total_workload_is_not_multiplied_by_days()
    {
        EvaluatedDay first = Day(1);
        EvaluatedDay second = Day(2);

        EvaluatedTimesheet timesheet = new(Year: 2026, Month: 6, CoreWorkload: 1m, Days: [first, second]);
        Assert.Equal(1m, timesheet.TotalWorkload);
    }

    private static AttendanceDay Day(DateTime date, TimeSpan? clockIn = null, TimeSpan? clockOut = null, TimeSpan? breakStart = null, TimeSpan? breakEnd = null, bool isHoliday = false, string? otherInterruption = null) => new(Date: date, ClockIn: clockIn, ClockOut: clockOut, BreakStart: breakStart, BreakEnd: breakEnd, OtherInterruption: otherInterruption, Schedules: [], IsHoliday: isHoliday, Workload: 1m);

    private static EvaluatedDay Day(int day) => new(Date: new DateTime(2026, 6, day, 0, 0, 0, DateTimeKind.Utc), IsHoliday: false, Workload: 1m, CoreWorkload: 1m, WorkedHours: 8m, CoreHours: 8m, ContractPartHours: 0m, StagHours: 0m, HasAttendanceFilled: true, SkipAllocationRules: false);

    private static ContractPartColumn Project(Guid id, decimal workload) =>
        new(id, workload, Locked: false, Range: new ContractPartDateRange(DateTime.MinValue, null));

    private static EditableTimesheetDay EditableDay(
        DateTime date,
        string description,
        decimal coreHours = 0m,
        Dictionary<Guid, decimal>? projectHours = null,
        Dictionary<Guid, bool>? projectLocks = null) => new()
        {
            Date = date,
            ClockIn = null,
            ClockOut = null,
            BreakStart = null,
            BreakEnd = null,
            Description = description,
            Schedules = [],
            IsHoliday = false,
            CoreHours = coreHours,
            CoreHoursFixed = false,
            ContractPartHours = projectHours ?? [],
            ContractPartHoursFixed = projectLocks ?? [],
            ContractPartHoursFloor = projectHours?.ToDictionary(pair => pair.Key, _ => 0m) ?? []
        };
}
