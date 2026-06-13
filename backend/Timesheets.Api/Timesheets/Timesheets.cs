namespace Timesheets.Api.Timesheets;

public sealed record TimeRange(TimeSpan Start, TimeSpan End);

public interface ISummarizable
{
    decimal TotalWorkload { get; }
    decimal TotalHours { get; }
    decimal TotalHoursObligation { get; }
}
public interface ITimesheet : ISummarizable
{
    public int Year { get; }
    public int Month { get; }
}
public interface ITimesheet<T> : ITimesheet where T : IDay
{
    public IReadOnlyList<T> Days { get; }
}
public interface IDay : ISummarizable
{
    public DateTime Date { get; init; }
    bool IsHoliday { get; init; }
    bool IsWeekend { get; }
    bool IsWorkday { get; }
}

public sealed record CombinedTimesheet(
    int Year,
    int Month,
    decimal CoreWorkload,
    IReadOnlyList<CombinedDay> Days
) : ITimesheet<CombinedDay>
{
    public decimal TotalHours => Days.Sum(d => d.TotalHours);
    public decimal TotalWorkload => Days.Sum(d => d.TotalWorkload);
    public decimal TotalHoursObligation => TimesheetLogic.CalculateTotalHoursObligation(Days);
}

public sealed record CombinedDay(
    DateTime Date,
    bool IsHoliday,
    decimal Workload,
    decimal CoreWorkload,
    decimal WorkedHours,
    decimal CoreHours,
    decimal ProjectHours,
    decimal StagHours,
    bool HasAttendanceFilled,
    bool SkipAllocationRules
) : IDay
{
    public bool IsWeekend => TimesheetLogic.IsWeekend(this);
    public bool IsWorkday => TimesheetLogic.IsWorkday(this);
    public decimal TotalWorkload => Workload;
    public decimal TotalHours => WorkedHours;
    public decimal AllocatedHours => TimesheetLogic.Normalize(CoreHours + ProjectHours);
    public decimal TotalHoursObligation => TimesheetLogic.CalculateTotalHoursObligation(this);
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
public sealed record AttendanceTimesheet(
    string EmployeePersonalNumber,
    string? EmployeeName,
    decimal Workload,
    int Year,
    int Month,
    IReadOnlyList<AttendanceDay> Days
) : ITimesheet<AttendanceDay>
{
    public decimal TotalWorkload => Workload;
    public decimal TotalHours => TimesheetLogic.CalculateTotalHours(Days);
    public decimal TotalHoursObligation => TimesheetLogic.CalculateTotalHoursObligation(Days);
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
public sealed record AttendanceDay(
    DateTime Date,
    TimeSpan? ClockIn,
    TimeSpan? ClockOut,
    TimeSpan? BreakStart,
    TimeSpan? BreakEnd,
    string? OtherInterruption,
    IReadOnlyList<TimeRange> Schedules,
    bool IsHoliday,
    decimal Workload
) : IDay
{
    public bool IsWeekend => TimesheetLogic.IsWeekend(this);
    public bool IsWorkday => TimesheetLogic.IsWorkday(this);

    public decimal TotalWorkload => Workload;
    public decimal TotalHoursObligation => TimesheetLogic.CalculateTotalHoursObligation(this);
    public decimal TotalHours => TimesheetLogic.CalculateTotalHours(this);
}

/// <summary>
/// Měsíční výkaz projektové činnosti.
/// </summary>
/// <param name="Year">Rok vykazovaného období.</param>
/// <param name="Month">Měsíc vykazovaného období.</param>
/// <param name="Workload">Úvazek.</param>
/// <param name="Days">Dny měsíčního výkazu projektové činnosti.</param>
public sealed record ProjectTimesheet(
    int Year,
    int Month,
    decimal Workload,
    IReadOnlyList<ProjectDay> Days
) : ITimesheet<ProjectDay>
{
    public decimal TotalWorkload => Workload;
    public decimal TotalHours => TimesheetLogic.CalculateTotalHours(Days);
    public decimal TotalHoursObligation => TimesheetLogic.CalculateTotalHoursObligation(Days);
}

/// <summary>
/// Den měsíčního výkazu projektové činnosti.
/// </summary>
/// <param name="Date">Datum.</param>
/// <param name="Hours">Počet hodin.</param>
/// <param name="IsHoliday">Určuje, zda se jedná o státní svátek.</param>
public sealed record ProjectDay(
    DateTime Date,
    decimal Hours,
    bool IsHoliday,
    decimal Workload
) : IDay
{
    public bool IsWeekend => TimesheetLogic.IsWeekend(this);
    public bool IsWorkday => TimesheetLogic.IsWorkday(this);

    public decimal TotalWorkload => Workload;
    public decimal TotalHoursObligation => TimesheetLogic.CalculateTotalHoursObligation(this);
    public decimal TotalHours => Hours;
}

public static class TimesheetLogic
{
    private const decimal StandardWorkdayHours = 8m;

    public static bool IsWeekend(IDay day) => day.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    public static bool IsWorkday(IDay day) => !IsWeekend(day) && !day.IsHoliday;
    public static bool HasObligation(IDay day) => !IsWeekend(day);

    public static decimal CalculateTotalHoursObligation(IDay day) => HasObligation(day) ? Normalize(StandardWorkdayHours * day.TotalWorkload) : 0m;
    public static decimal CalculateTotalHoursObligation(IEnumerable<IDay> days) => days.Sum(day => day.TotalHoursObligation);

    public static decimal CalculateTotalHours(IEnumerable<IDay> days) => days.Sum(day => day.TotalHours);
    public static decimal CalculateTotalHours(AttendanceDay day) =>
        CalculateWorkedHoursFromAttendance(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd);

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

    public static bool HasUnequalHours(decimal left, decimal right) =>
        Math.Abs(Normalize(left) - Normalize(right)) >= 0.01m;

    public static decimal Normalize(decimal hours)
    {
        decimal clamped = Math.Max(hours, 0);
        return Math.Round(clamped, decimals: 2, MidpointRounding.AwayFromZero);
    }
}
