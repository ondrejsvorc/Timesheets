namespace Timesheets.Api.Timesheets;

public sealed record TimeRange(TimeSpan Start, TimeSpan End);

public sealed record CombinedTimesheet(int Year, int Month, decimal CoreWorkload, IReadOnlyList<CombinedDay> Days)
{
    public decimal TotalHours => Days.Sum(d => d.TotalHours);
    public decimal TotalWorkload => Days.FirstOrDefault()?.TotalWorkload ?? 0m;
    public decimal TotalHoursObligation => Days.Sum(day => day.TotalHoursObligation);
}

public sealed record CombinedDay(DateTime Date, bool IsHoliday, decimal Workload, decimal CoreWorkload, decimal WorkedHours, decimal CoreHours, decimal ProjectHours, decimal StagHours, bool HasAttendanceFilled, bool SkipAllocationRules)
{
    public bool IsWeekend => TimesheetLogic.IsWeekend(Date);
    public bool IsWorkday => TimesheetLogic.IsWorkday(Date, IsHoliday);
    public decimal TotalWorkload => Workload;
    public decimal AllocatedHours => TimesheetLogic.Normalize(CoreHours + ProjectHours);
    public decimal TotalHours => HasAttendanceFilled ? WorkedHours : AllocatedHours;
    public decimal TotalHoursObligation => TimesheetLogic.CalculateTotalHoursObligation(Date, IsHoliday, Workload);
}

/// <summary>
/// Měsíční výkaz pracovní doby.
/// </summary>
/// <param name="EmployeePersonalNumber">Osobní číslo zaměstnance.</param>
/// <param name="EmployeeName">Celé jméno zaměstnance, včetně titulů.</param>
/// <param name="Workload">Úvazek.</param>
/// <param name="Year">Rok vykazovaného období.</param>
/// <param name="Month">Měsíc vykazovaného období.</param>
/// <param name="Days">Dny měsíčního výkazu pracovní doby.</param>
public sealed record AttendanceTimesheet(string EmployeePersonalNumber, string? EmployeeName, decimal Workload, int Year, int Month, IReadOnlyList<AttendanceDay> Days)
{
    public decimal TotalWorkload => Workload;
    public decimal TotalHours => Days.Sum(day => day.TotalHours);
    public decimal TotalHoursObligation => Days.Sum(day => day.TotalHoursObligation);
}

/// <summary>
/// Den v měsíčním výkazu pracovní doby.
/// </summary>
/// <param name="Date">Datum.</param>
/// <param name="ClockIn">Příchod.</param>
/// <param name="ClockOut">Odchod.</param>
/// <param name="BreakStart">Začátek přestávky.</param>
/// <param name="BreakEnd">Konec přestávky.</param>
/// <param name="OtherInterruption">Jiné přerušení (úvazek).</param>
/// <param name="IsHoliday">Určuje, zda se jedná o státní svátek.</param>
/// <param name="Workload">Úvazek.</param>
public sealed record AttendanceDay(DateTime Date, TimeSpan? ClockIn, TimeSpan? ClockOut, TimeSpan? BreakStart, TimeSpan? BreakEnd, string? OtherInterruption, IReadOnlyList<TimeRange> Schedules, bool IsHoliday, decimal Workload)
{
    public bool IsWeekend => TimesheetLogic.IsWeekend(Date);
    public bool IsWorkday => TimesheetLogic.IsWorkday(Date, IsHoliday);

    public decimal TotalWorkload => Workload;
    public decimal TotalHoursObligation => TimesheetLogic.CalculateTotalHoursObligation(Date, IsHoliday, Workload);
    public decimal TotalHours => TimesheetLogic.CalculateWorkedHoursFromAttendance(ClockIn, ClockOut, BreakStart, BreakEnd);
}

/// <summary>
/// Měsíční výkaz projektové činnosti.
/// </summary>
/// <param name="Year">Rok vykazovaného období.</param>
/// <param name="Month">Měsíc vykazovaného období.</param>
/// <param name="Workload">Úvazek.</param>
/// <param name="Days">Dny měsíčního výkazu projektové činnosti.</param>
public sealed record ProjectTimesheet(int Year, int Month, decimal Workload, IReadOnlyList<ProjectDay> Days)
{
    public decimal TotalWorkload => Workload;
    public decimal TotalHours => Days.Sum(day => day.TotalHours);
    public decimal TotalHoursObligation => Days.Sum(day => day.TotalHoursObligation);
}

/// <summary>
/// Den měsíčního výkazu projektové činnosti.
/// </summary>
/// <param name="Date">Datum.</param>
/// <param name="Hours">Počet hodin.</param>
/// <param name="IsHoliday">Určuje, zda se jedná o státní svátek.</param>
public sealed record ProjectDay(DateTime Date, decimal Hours, bool IsHoliday, decimal Workload)
{
    public bool IsWeekend => TimesheetLogic.IsWeekend(Date);
    public bool IsWorkday => TimesheetLogic.IsWorkday(Date, IsHoliday);

    public decimal TotalWorkload => Workload;
    public decimal TotalHoursObligation => TimesheetLogic.CalculateTotalHoursObligation(Date, IsHoliday, Workload);
    public decimal TotalHours => Hours;
}

public static class TimesheetLogic
{
    /// <summary>
    /// Zákon č. 262/2006 Sb., zákoník práce — § 79 odst. 1
    /// Standardní denní pracovní doba činí 8 hodin (při úvazku 1,0).
    /// </summary>
    private const decimal StandardWorkdayHours = 8m;

    public static bool IsWeekend(DateTime date) => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    public static bool IsWeekday(DateTime date) => !IsWeekend(date);
    public static bool IsWorkday(DateTime date, bool isHoliday) => date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday && !isHoliday;
    public static decimal CalculateTotalHoursObligation(DateTime date, bool isHoliday, decimal workload) => IsWeekday(date) ? Normalize(StandardWorkdayHours * workload) : 0m;

    public static decimal CalculateWorkedHoursFromAttendance(TimeSpan? clockIn, TimeSpan? clockOut, TimeSpan? breakStart, TimeSpan? breakEnd)
    {
        decimal workedHours = CalculateWorkedHours(clockIn, clockOut);
        decimal breakHours = CalculateBreakHours(breakStart, breakEnd, clockIn, clockOut);
        return Normalize(Math.Max(0, workedHours - breakHours));
    }

    public static decimal CalculateWorkedHours(TimeSpan? clockIn, TimeSpan? clockOut)
    {
        if (clockIn is null || clockOut is null)
        {
            return 0;
        }

        int clockInMinutes = (int)Math.Round(clockIn.Value.TotalMinutes);
        int clockOutMinutes = (int)Math.Round(clockOut.Value.TotalMinutes);

        if (clockOutMinutes > clockInMinutes)
        {
            return Normalize((clockOutMinutes - clockInMinutes) / 60m);
        }

        if (clockOutMinutes < clockInMinutes)
        {
            int workedMinutes = clockOutMinutes + 24 * 60 - clockInMinutes;
            if (workedMinutes > 12 * 60)
            {
                return 0;
            }

            return Normalize(workedMinutes / 60m);
        }

        return 0;
    }

    public static decimal CalculateElapsedHours(TimeSpan? start, TimeSpan? end)
    {
        if (start is null || end is null)
        {
            return 0m;
        }

        int startMinutes = (int)Math.Round(start.Value.TotalMinutes);
        int endMinutes = (int)Math.Round(end.Value.TotalMinutes);
        int elapsed = endMinutes >= startMinutes ? endMinutes - startMinutes : endMinutes + 24 * 60 - startMinutes;
        return Normalize(elapsed / 60m);
    }

    public static decimal CalculateBreakHours(TimeSpan? breakStart, TimeSpan? breakEnd, TimeSpan? clockIn, TimeSpan? clockOut)
    {
        if (breakStart is null || breakEnd is null)
        {
            return 0;
        }

        int breakStartMinutes = (int)Math.Round(breakStart.Value.TotalMinutes);
        int breakEndMinutes = (int)Math.Round(breakEnd.Value.TotalMinutes);

        if (breakEndMinutes > breakStartMinutes)
        {
            return Normalize((breakEndMinutes - breakStartMinutes) / 60m);
        }

        if (breakEndMinutes < breakStartMinutes)
        {
            int breakMinutes = breakEndMinutes + 24 * 60 - breakStartMinutes;
            if (breakMinutes <= 12 * 60)
            {
                return Normalize(breakMinutes / 60m);
            }
        }

        return 0;
    }

    public static decimal CalculateStagHours(IReadOnlyList<TimeRange> schedules)
    {
        if (schedules.Count == 0)
        {
            return 0;
        }

        int totalMinutes = 0;
        foreach (TimeRange schedule in schedules)
        {
            int start = (int)Math.Round(schedule.Start.TotalMinutes);
            int end = (int)Math.Round(schedule.End.TotalMinutes);
            if (end > start)
            {
                totalMinutes += end - start;
            }
        }

        decimal hours = totalMinutes / 60m;
        return Normalize(Math.Min(12m, hours));
    }

    public static decimal CalculateNightHours(TimeSpan? clockIn, TimeSpan? clockOut, TimeSpan? breakStart, TimeSpan? breakEnd)
    {
        if (clockIn is null || clockOut is null)
        {
            return 0m;
        }

        int shiftStart = (int)Math.Round(clockIn.Value.TotalMinutes);
        int shiftEnd = (int)Math.Round(clockOut.Value.TotalMinutes);
        if (shiftEnd < shiftStart)
        {
            shiftEnd += 24 * 60;
        }

        int nightMinutes = NightOverlap(shiftStart, shiftEnd);
        if (breakStart is not null && breakEnd is not null)
        {
            int pauseStart = (int)Math.Round(breakStart.Value.TotalMinutes);
            int pauseEnd = (int)Math.Round(breakEnd.Value.TotalMinutes);
            if (pauseEnd < pauseStart)
            {
                pauseEnd += 24 * 60;
            }
            if (pauseStart < shiftStart)
            {
                pauseStart += 24 * 60;
                pauseEnd += 24 * 60;
            }
            nightMinutes -= NightOverlap(pauseStart, pauseEnd);
        }

        return Normalize(Math.Max(0, nightMinutes) / 60m);
    }

    public static bool HasUnequalHours(decimal left, decimal right) => Math.Abs(Normalize(left) - Normalize(right)) >= 0.01m;

    public static decimal Round(decimal hours) => Math.Round(hours, decimals: 2, MidpointRounding.AwayFromZero);

    public static decimal Normalize(decimal hours)
    {
        decimal clamped = Math.Max(hours, 0);
        return Round(clamped);
    }

    private static int NightOverlap(int start, int end)
    {
        (int Start, int End)[] intervals =
        [
            (0, 6 * 60),
            (22 * 60, 24 * 60),
            (24 * 60, 30 * 60),
            (46 * 60, 48 * 60)
        ];

        return intervals.Sum(interval => Math.Max(0, Math.Min(end, interval.End) - Math.Max(start, interval.Start)));
    }
}

internal static class TimesheetInterruptions
{
    private static readonly HashSet<string> BusinessTripCodes = ["SCP", "SCS", "SCT", "SCZ", "SCZE", "SCZP", "SCZS"];
    private static readonly HashSet<string> CoreOnlyCodes = ["M", "NK", "NL"];

    private static string[] ParseCodes(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(code => code.ToUpperInvariant()).ToArray();
    }

    public static bool HasBusinessTripInterruption(string? raw) => ParseCodes(raw).Any(BusinessTripCodes.Contains);

    public static bool HasCoreOnlyInterruption(string? raw) => ParseCodes(raw).Any(CoreOnlyCodes.Contains);

    public static bool HasProportionalInterruption(string? raw)
    {
        string[] codes = ParseCodes(raw);
        return codes.Length > 0 && !codes.Any(BusinessTripCodes.Contains) && !codes.Any(CoreOnlyCodes.Contains);
    }

    public static bool SkipAllocationRules(string? raw) => HasBusinessTripInterruption(raw) || HasProportionalInterruption(raw);
}
