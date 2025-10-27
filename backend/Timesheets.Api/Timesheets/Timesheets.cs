namespace Timesheets.Api.Timesheets;

public interface ITimesheet
{
    public int Year { get; init; }
    public int Month { get; init; }
}
public interface IDay
{
    public DateOnly Date { get; init; }
}

/// <summary>
/// Měsíční výkaz pracovní doby.
/// </summary>
/// <param name="EmployeePersonalNumber">Osobní číslo zaměstnance.</param>
/// <param name="EmployeeName">Celé jméno zaměstnance, včetně titulů.</param>
/// <param name="Year">Rok vykazovaného období.</param>
/// <param name="Month">Měsíc vykazovaného období.</param>
/// <param name="Days">Dny měsíčního výkazu pracovní doby.</param>
public sealed record AttendanceTimesheet(
    int EmployeePersonalNumber,
    string? EmployeeName,
    int Year,
    int Month,
    IReadOnlyList<AttendanceDay> Days
) : ITimesheet
{
    /// <summary>
    /// Součet všech hodin bez přestávky.
    /// </summary>
    public decimal TotalHoursWithoutBreak => Days.Sum(r => r.HoursWithoutBreak ?? 0);

    /// <summary>
    /// Součet všech hodin povinnosti pouze za pracovní dny.
    /// Svátky a víkendy se do fondu nezapočítávají.
    /// </summary>
    public decimal TotalHoursObligation => Days.Where(d => d.IsWorkDay).Sum(d => d.HoursObligation ?? 0);
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
/// <param name="HoursWithoutBreak">Celkem od - do bez přestávky na jídlo.</param>
/// <param name="HoursObligation">Denní povinnost v hodinách.</param>
/// <param name="IsHoliday">Určuje, zda se jedná o státní svátek.</param>
public sealed record AttendanceDay(
    DateOnly Date,
    TimeOnly? ClockIn,
    TimeOnly? ClockOut,
    TimeOnly? BreakStart,
    TimeOnly? BreakEnd,
    string? OtherInterruption,
    decimal? HoursWithoutBreak,
    decimal? HoursObligation,
    bool IsHoliday
) : IDay
{
    /// <summary>
    /// Určuje, zda den připadá na víkend.
    /// </summary>
    public bool IsWeekend => Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    /// <summary>
    /// Určuje, zda se jedná o pracovní den (nikoli víkend nebo svátek).
    /// </summary>
    public bool IsWorkDay => !IsWeekend && !IsHoliday;
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
/// <param name="EmployerName">Název zaměstnavatele, u kterého je sjednaná pozice.</param>
/// <param name="PositionName">Název pozice.</param>
/// <param name="WorkloadPercent">Výše úvazku u zaměstnavatele (%).</param>
/// <param name="Days">Dny měsíčního výkazu projektové činnosti.</param>
public sealed record ProjectTimesheet(
    string? EmployeeName,
    int Year,
    int Month,
    string? ProjectName,
    string? RecipientName,
    string? ProjectRegistrationNumber,
    string? EmployerName,
    string? PositionName,
    decimal? WorkloadPercent,
    IReadOnlyList<ProjectDay> Days
) : ITimesheet
{
    /// <summary>
    /// Součet odpracovaných hodin.
    /// </summary>
    public decimal TotalHours => Days.Sum(r => r.Hours ?? 0);
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
    bool IsHoliday
) : IDay
{
    /// <summary>
    /// Určuje, zda den připadá na víkend.
    /// </summary>
    public bool IsWeekend => Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    /// <summary>
    /// Určuje, zda se jedná o pracovní den (nikoli víkend nebo svátek).
    /// </summary>
    public bool IsWorkDay => !IsWeekend && !IsHoliday;
}