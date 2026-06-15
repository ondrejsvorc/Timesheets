namespace Timesheets.Api.Timesheets;

public sealed record TimesheetMonthSummary(DateTime PeriodStart, DateTime PeriodEnd, int Workdays, int VacationDays, int SickDays, int Holidays, decimal TotalWorkload);
public sealed record TimesheetMonthSummaryDay(DateTime Date, bool IsHoliday, string? Description);

public static class TimesheetMonthSummaryCalculator
{
    private static readonly HashSet<string> VacationCodes = ["D"];
    private static readonly HashSet<string> SickCodes = ["N", "NL", "NP", "O", "ZV"];

    public static TimesheetMonthSummary Compute(int year, int month, IReadOnlyList<TimesheetMonthSummaryDay> days, decimal totalWorkload)
    {
        DateTime periodStart = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime periodEnd = periodStart.AddMonths(1).AddDays(-1);

        int workdays = days.Count(day => TimesheetLogic.IsWeekday(day.Date));
        int vacationDays = days.Count(day => HasInterruptionCode(day.Description, VacationCodes));
        int sickDays = days.Count(day => HasInterruptionCode(day.Description, SickCodes));
        int holidays = days.Count(day => day.IsHoliday);

        return new TimesheetMonthSummary(periodStart, periodEnd, workdays, vacationDays, sickDays, holidays, totalWorkload);
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
