namespace Timesheets.Api.Timesheets;

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
    public DateOnly Date { get; }
    bool IsHoliday { get; }
    bool IsWeekend { get; }
    bool IsWorkday { get; }
}

public sealed record CombinedTimesheet(
    int Year,
    int Month,
    IReadOnlyList<CombinedDay> Days
) : ITimesheet<CombinedDay>
{
    public decimal TotalHours => Days.Sum(d => d.TotalHours);
    public decimal TotalWorkload => Days.Sum(d => d.TotalWorkload);
    public decimal TotalHoursObligation => TimesheetLogic.CalculateTotalHoursObligation(Days);
}

public sealed record CombinedDay(
    DateOnly Date,
    bool IsHoliday,
    bool IsWeekend,
    bool IsWorkday,
    decimal AttendanceHours,
    decimal ProjectHours,
    decimal AttendanceWorkload,
    decimal ProjectWorkload
) : IDay
{
    public decimal TotalHours => AttendanceHours + ProjectHours;
    public decimal TotalWorkload => AttendanceWorkload + ProjectWorkload;
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
    int EmployeePersonalNumber,
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
    DateOnly Date,
    TimeOnly? ClockIn,
    TimeOnly? ClockOut,
    TimeOnly? BreakStart,
    TimeOnly? BreakEnd,
    string? OtherInterruption,
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
    DateOnly Date,
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

file static class TimesheetLogic
{
    private const decimal StandardWorkdayHours = 8m;

    public static bool IsWeekend(IDay day) => day.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    public static bool IsWorkday(IDay day) => !IsWeekend(day) && !day.IsHoliday;
    public static bool HasObligation(IDay day) => !IsWeekend(day);

    public static decimal CalculateTotalHoursObligation(IDay day) => HasObligation(day) ? Normalize(StandardWorkdayHours * day.TotalWorkload) : 0m;
    public static decimal CalculateTotalHoursObligation(IEnumerable<IDay> days) => days.Sum(day => day.TotalHoursObligation);

    public static decimal CalculateTotalHours(IEnumerable<IDay> days) => days.Sum(day => day.TotalHours);
    public static decimal CalculateTotalHours(AttendanceDay day)
    {
        decimal workedHours = CalculateWorkedHours(day.ClockIn, day.ClockOut);
        decimal breakHours = CalculateBreakHours(day.BreakStart, day.BreakEnd);
        decimal hours = workedHours - breakHours;
        return Normalize(hours);
    }

    private static decimal CalculateWorkedHours(TimeOnly? clockIn, TimeOnly? clockOut)
    {
        if (clockIn is null || clockOut is null || clockOut <= clockIn)
        {
            return 0;
        }
        return (decimal)(clockOut.Value - clockIn.Value).TotalHours;
    }

    private static decimal CalculateBreakHours(TimeOnly? breakStart, TimeOnly? breakEnd)
    {
        if (breakStart is null || breakEnd is null || breakEnd <= breakStart)
        {
            return 0;
        }
        return (decimal)(breakEnd.Value - breakStart.Value).TotalHours;
    }

    private static decimal Normalize(decimal hours)
    {
        decimal clamped = Math.Max(hours, 0);
        return Math.Round(clamped, decimals: 2, MidpointRounding.AwayFromZero);
    }
}
