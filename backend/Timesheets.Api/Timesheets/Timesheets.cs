namespace Timesheets.Api.Timesheets;

/// <summary>
/// Měsíční výkaz pracovní doby.
/// </summary>
public sealed class AttendanceTimesheet
{
    /// <summary>
    /// Osobní číslo zaměstnance.
    /// </summary>
    public int EmployeePersonalNumber { get; init; }

    /// <summary>
    /// Celé jméno zaměstnance, včetně titulů.
    /// </summary>
    public string? EmployeeName { get; init; }

    /// <summary>
    /// Rok vykazovaného období.
    /// </summary>
    public int Year { get; init; }

    /// <summary>
    /// Měsíc vykazovaného období.
    /// </summary>
    public int Month { get; init; }

    /// <summary>
    /// Dny měsíčního výkazu pracovní doby.
    /// </summary>
    public IReadOnlyList<AttendanceDay> Days { get; init; } = [];

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
public sealed class AttendanceDay
{
    /// <summary>
    /// Datum.
    /// </summary>
    public DateOnly Date { get; init; }

    /// <summary>
    /// Příchod.
    /// </summary>
    public TimeOnly? ClockIn { get; init; }

    /// <summary>
    /// Odchod.
    /// </summary>
    public TimeOnly? ClockOut { get; init; }

    /// <summary>
    /// Začátek přestávky.
    /// </summary>
    public TimeOnly? BreakStart { get; init; }

    /// <summary>
    /// Konec přestávky.
    /// </summary>
    public TimeOnly? BreakEnd { get; init; }

    /// <summary>
    /// Jiné přerušení (úvazek).
    /// </summary>
    public string? OtherInterruption { get; init; }

    /// <summary>
    /// Celkem od - do bez přestávky na jídlo.
    /// </summary>
    public decimal? HoursWithoutBreak { get; init; }

    /// <summary>
    /// Denní povinnost v hodinách.
    /// </summary>
    public decimal? HoursObligation { get; init; }

    public bool IsWeekend => Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    public required bool IsHoliday { get; init; }
    public bool IsWorkDay => !IsWeekend && !IsHoliday;
}

/// <summary>
/// Měsíční výkaz projektové činnosti.
/// </summary>
public sealed class ProjectTimesheet
{
    /// <summary>
    /// Celé jméno zaměstnance, včetně titulů.
    /// </summary>
    public string? EmployeeName { get; init; }

    /// <summary>
    /// Rok vykazovaného období.
    /// </summary>
    public int Year { get; init; }

    /// <summary>
    /// Měsíc vykazovaného období.
    /// </summary>
    public int Month { get; init; }

    /// <summary>
    /// Název projektu.
    /// </summary>
    public string? ProjectName { get; init; }

    /// <summary>
    /// Název příjemce.
    /// </summary>
    public string? RecipientName { get; init; }

    /// <summary>
    /// Registrační číslo projektu.
    /// </summary>
    public string? ProjectRegistrationNumber { get; init; }

    /// <summary>
    /// Název zaměstnavatele, u kterého je sjednaná pozice.
    /// </summary>
    public string? EmployerName { get; init; }

    /// <summary>
    /// Název pozice.
    /// </summary>
    public string? PositionName { get; init; }

    /// <summary>
    /// Výše úvazku u zaměstnavatele (%).
    /// </summary>
    public decimal? WorkloadPercent { get; init; }

    /// <summary>
    /// Dny měsíčního výkazu projektové činnosti.
    /// </summary>
    public IReadOnlyList<ProjectDay> Days { get; init; } = [];

    /// <summary>
    /// Součet odpracovaných hodin.
    /// </summary>
    public decimal TotalHours => Days.Sum(r => r.Hours ?? 0);
}

/// <summary>
/// Den měsíčního výkazu projektové činnosti.
/// </summary>
public sealed class ProjectDay
{
    /// <summary>
    /// Datum.
    /// </summary>
    public DateOnly Date { get; init; }

    /// <summary>
    /// Klíčová aktivita.
    /// </summary>
    public string? ActivityKey { get; init; }

    /// <summary>
    /// Název skupiny činností.
    /// </summary>
    public string? ActivityGroup { get; init; }

    /// <summary>
    /// Popis činností včetně průběžných výstupů práce za daný měsíc.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Počet hodin.
    /// </summary>
    public decimal? Hours { get; init; }

    public bool IsWeekend => Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    public required bool IsHoliday { get; init; }
    public bool IsWorkDay => !IsWeekend && !IsHoliday;
}