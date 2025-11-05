namespace Timesheets.Api.Timesheets;

public interface ITimesheet
{
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal Workload { get; init; }
}
public interface ITimesheet<T> : ITimesheet where T : IDay
{
    public IReadOnlyList<T> Days { get; init; }
}
public interface IDay
{
    public DateOnly Date { get; init; }
    bool IsHoliday { get; init; }
    bool IsWeekend { get; }
    bool IsWorkDay { get; }
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
    /// <summary>
    /// Součet všech hodin bez přestávky.
    /// </summary>
    public decimal TotalHoursWithoutBreak => TimesheetLogic.CalculateTotalHoursWithoutBreak(Days);

    /// <summary>
    /// Součet všech hodin povinnosti pouze za pracovní dny.
    /// Svátky a víkendy se do fondu nezapočítávají.
    /// </summary>
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
    public bool IsWorkDay => TimesheetLogic.IsWorkDay(this);

    /// <summary>
    /// Denní povinnost v hodinách.
    /// Vypočítává se na základě úvazku.
    /// </summary>
    public decimal HoursObligation => TimesheetLogic.CalculateHoursObligation(this);

    /// <summary>
    /// Celkem od - do bez přestávky na jídlo.
    /// </summary>
    public decimal HoursWithoutBreak => TimesheetLogic.CalculateHoursWithoutBreak(this);
}

/// <summary>
/// Měsíční výkaz projektové činnosti.
/// </summary>
/// <param name="EmployeeName">Celé jméno zaměstnance, včetně titulů.</param>
/// <param name="Year">Rok vykazovaného období.</param>
/// <param name="Month">Měsíc vykazovaného období.</param>
/// <param name="ProjectName">Název projektu.</param>
/// <param name="RecipientName">Název příjemce.</param>
/// <param name="ProjectRegistrationNumber">Registrační číslo projektu.</param>
/// <param name="PositionName">Název pozice.</param>
/// <param name="Workload">Úvazek.</param>
/// <param name="Days">Dny měsíčního výkazu projektové činnosti.</param>
public sealed record ProjectTimesheet(
    string? EmployeeName,
    int Year,
    int Month,
    string? ProjectName,
    string? RecipientName,
    string? ProjectRegistrationNumber,
    string? PositionName,
    decimal Workload,
    IReadOnlyList<ProjectDay> Days
) : ITimesheet<ProjectDay>
{
    // TODO
    public decimal TotalHours => Days.Sum(day => day.Hours ?? 0);
}

/// <summary>
/// Den měsíčního výkazu projektové činnosti.
/// </summary>
/// <param name="Date">Datum.</param>
/// <param name="ActivityKey">Klíčová aktivita.</param>
/// <param name="ActivityGroup">Název skupiny činností.</param>
/// <param name="Description">Popis činností včetně průběžných výstupů práce za daný měsíc.</param>
/// <param name="Hours">Počet hodin.</param>
/// <param name="IsHoliday">Určuje, zda se jedná o státní svátek.</param>
public sealed record ProjectDay(
    DateOnly Date,
    string? ActivityKey,
    string? ActivityGroup,
    string? Description,
    decimal? Hours,
    bool IsHoliday,
    decimal Workload
) : IDay
{
    public bool IsWeekend => TimesheetLogic.IsWeekend(this);
    public bool IsWorkDay => TimesheetLogic.IsWorkDay(this);
}

file static class TimesheetLogic
{
    private const decimal StandardWorkdayHours = 8m;

    public static bool IsWeekend(IDay day) => day.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    public static bool IsWorkDay(IDay day) => !IsWeekend(day) && !day.IsHoliday;

    public static decimal CalculateWorkedHours(TimeOnly? clockIn, TimeOnly? clockOut)
    {
        if (clockIn is null || clockOut is null || clockOut <= clockIn)
        {
            return 0;
        }
        return (decimal)(clockOut.Value - clockIn.Value).TotalHours;
    }

    public static decimal CalculateBreakHours(TimeOnly? breakStart, TimeOnly? breakEnd)
    {
        if (breakStart is null || breakEnd is null || breakEnd <= breakStart)
        {
            return 0;
        }
        return (decimal)(breakEnd.Value - breakStart.Value).TotalHours;
    }

    public static decimal CalculateHoursWithoutBreak(AttendanceDay day)
    {
        decimal workedHours = CalculateWorkedHours(day.ClockIn, day.ClockOut);
        decimal breakHours = CalculateBreakHours(day.BreakStart, day.BreakEnd);
        decimal hours = workedHours - breakHours;
        return Normalize(hours);
    }

    public static decimal CalculateHoursObligation(AttendanceDay day)
    {
        if (!IsWorkDay(day))
        {
            return 0;
        }
        decimal hours = StandardWorkdayHours * day.Workload;
        return Normalize(hours);
    }

    public static decimal CalculateTotalHoursObligation(IEnumerable<AttendanceDay> days)
    {
        return days.Sum(CalculateHoursObligation);
    }

    public static decimal CalculateTotalHoursWithoutBreak(IEnumerable<AttendanceDay> days)
    {
        return days.Sum(CalculateHoursWithoutBreak);
    }

    private static decimal Normalize(decimal hours)
    {
        decimal clamped = Math.Max(hours, 0);
        return Math.Round(clamped, decimals: 2, MidpointRounding.AwayFromZero);
    }
}
