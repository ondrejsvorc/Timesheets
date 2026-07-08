using Timesheets.Api.Features.Timesheets;

namespace Timesheets.Api.Tests.Unit;

public sealed class TimesheetMonthSummaryTests
{
    [Fact]
    public void Compute_counts_workdays_vacation_sick_and_holidays()
    {
        DateTime monthStart = new(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        List<TimesheetMonthSummaryDay> days =
        [
            new(monthStart, IsHoliday: false, Description: null),
            new(monthStart.AddDays(1), IsHoliday: false, Description: "D"),
            new(monthStart.AddDays(2), IsHoliday: true, Description: null),
            new(monthStart.AddDays(3), IsHoliday: false, Description: "NL N (0)"),
            new(monthStart.AddDays(4), IsHoliday: false, Description: "NK"),
            new(monthStart.AddDays(5), IsHoliday: false, Description: null),
            new(monthStart.AddDays(6), IsHoliday: false, Description: null),
        ];

        TimesheetMonthSummary summary = TimesheetMonthSummaryCalculator.Compute(2025, 10, days, 1m);

        Assert.Equal(new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc), summary.PeriodStart);
        Assert.Equal(new DateTime(2025, 10, 31, 0, 0, 0, DateTimeKind.Utc), summary.PeriodEnd);
        Assert.Equal(4, summary.Workdays);
        Assert.Equal(1, summary.VacationDays);
        Assert.Equal(1, summary.SickDays);
        Assert.Equal(1, summary.Holidays);
        Assert.Equal(1m, summary.TotalWorkload);
    }

    [Fact]
    public void Compute_excludes_holidays_from_workdays()
    {
        List<TimesheetMonthSummaryDay> days = Enumerable.Range(1, 31)
            .Select(day => new TimesheetMonthSummaryDay(new DateTime(2026, 1, day, 0, 0, 0, DateTimeKind.Utc), IsHoliday: day == 1, Description: null))
            .ToList();

        TimesheetMonthSummary summary = TimesheetMonthSummaryCalculator.Compute(2026, 1, days, 1m);

        Assert.Equal(21, summary.Workdays);
        Assert.Equal(1, summary.Holidays);
    }

    [Fact]
    public void CalculateTotalHoursObligation_returns_zero_for_holiday()
    {
        decimal obligation = TimesheetLogic.CalculateTotalHoursObligation(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), isHoliday: true, workload: 1m);

        Assert.Equal(0m, obligation);
    }
}
