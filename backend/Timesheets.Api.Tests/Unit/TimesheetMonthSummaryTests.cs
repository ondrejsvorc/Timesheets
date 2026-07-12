using Timesheets.Api.Features.Timesheets;

namespace Timesheets.Api.Tests.Unit;

public sealed class TimesheetMonthSummaryTests
{
    private sealed record SummaryDay(DateTime Date, bool IsHoliday, string? Description);

    [Fact]
    public void Compute_counts_workdays_vacation_sick_and_holidays()
    {
        DateTime monthStart = new(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        List<SummaryDay> days =
        [
            new(monthStart, IsHoliday: false, Description: null),
            new(monthStart.AddDays(1), IsHoliday: false, Description: "D"),
            new(monthStart.AddDays(2), IsHoliday: true, Description: null),
            new(monthStart.AddDays(3), IsHoliday: false, Description: "NL N (0)"),
            new(monthStart.AddDays(4), IsHoliday: false, Description: "NK"),
            new(monthStart.AddDays(5), IsHoliday: false, Description: null),
            new(monthStart.AddDays(6), IsHoliday: false, Description: null),
        ];

        (DateTime periodStart, DateTime periodEnd, int workdays, int vacationDays, int sickDays, int holidays, decimal totalWorkload) = Compute(2025, 10, days, 1m);

        Assert.Equal(new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc), periodStart);
        Assert.Equal(new DateTime(2025, 10, 31, 0, 0, 0, DateTimeKind.Utc), periodEnd);
        Assert.Equal(4, workdays);
        Assert.Equal(1, vacationDays);
        Assert.Equal(1, sickDays);
        Assert.Equal(1, holidays);
        Assert.Equal(1m, totalWorkload);
    }

    [Fact]
    public void Compute_excludes_holidays_from_workdays()
    {
        List<SummaryDay> days = Enumerable.Range(1, 31)
            .Select(day => new SummaryDay(new DateTime(2026, 1, day, 0, 0, 0, DateTimeKind.Utc), IsHoliday: day == 1, Description: null))
            .ToList();

        (_, _, int workdays, _, _, int holidays, _) = Compute(2026, 1, days, 1m);

        Assert.Equal(21, workdays);
        Assert.Equal(1, holidays);
    }

    [Fact]
    public void CalculateTotalHoursObligation_returns_zero_for_holiday()
    {
        decimal obligation = TimesheetEvaluator.CalculateTotalHoursObligation(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), isHoliday: true, workload: 1m);

        Assert.Equal(0m, obligation);
    }

    private static (DateTime PeriodStart, DateTime PeriodEnd, int Workdays, int VacationDays, int SickDays, int Holidays, decimal TotalWorkload) Compute(int year, int month, IReadOnlyList<SummaryDay> days, decimal totalWorkload)
    {
        DateTime periodStart = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime periodEnd = periodStart.AddMonths(1).AddDays(-1);
        HashSet<string> vacationCodes = ["D"];
        HashSet<string> sickCodes = ["N", "NL", "NP", "O", "ZV"];

        int workdays = days.Count(day => TimesheetEvaluator.IsWorkday(day.Date, day.IsHoliday));
        int vacationDays = days.Count(day => HasInterruptionCode(day.Description, vacationCodes));
        int sickDays = days.Count(day => HasInterruptionCode(day.Description, sickCodes));
        int holidays = days.Count(day => day.IsHoliday);

        return (periodStart, periodEnd, workdays, vacationDays, sickDays, holidays, totalWorkload);
    }

    private static bool HasInterruptionCode(string? raw, HashSet<string> codes)
    {
        foreach (string code in ParseInterruptionCodes(raw))
        {
            if (codes.Contains(code))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> ParseInterruptionCodes(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            yield break;
        }

        foreach (string part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (string token in part.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                string code = token.Split('(')[0].ToUpperInvariant();
                if (code.Length > 0 && code.All(char.IsLetter))
                {
                    yield return code;
                }
            }
        }
    }
}
