using System.Globalization;

namespace Timesheets.Api.Timesheets;

// TODO: Placené svátky
// TODO: Práce o víkendu
// TODO: Přesčasy
// TODO: Pracovní neschopnost

public enum IssueType { Warning = 0, Error = 1 }

public sealed record TimesheetIssue(string Code, IssueType Type, string Description);
public sealed record DayIssue(string Code, IssueType Type, string Description, int Day, string Field);

public sealed class TimesheetReview
{
    public bool HasErrors => Issues.Any(i => i.Type is IssueType.Error) || DayIssues.Any(i => i.Type is IssueType.Error);
    public bool HasWarnings => Issues.Any(i => i.Type is IssueType.Warning) || DayIssues.Any(i => i.Type is IssueType.Warning);

    public bool CanBeSaved => !HasErrors;
    public bool CanBeApproved => !HasErrors && !HasWarnings;

    public IEnumerable<TimesheetIssue> Issues { get; init; } = [];
    public IEnumerable<DayIssue> DayIssues { get; init; } = [];
}

file static class TimesheetLimits
{
    /// <summary>
    /// Zákon č. 262/2006 Sb., zákoník práce — § 88 odst. 1
    /// Přestávka nejpozději po 6 hodinách nepřetržité práce.
    /// </summary>
    public const decimal MaxContinuousWorkBeforeBreakHours = 6;

    /// <summary>
    /// Zákon č. 262/2006 Sb., zákoník práce — § 83 odst. 4
    /// Délka směny nesmí přesáhnout 12 hodin.
    /// </summary>
    public const decimal MaxWorkShiftHours = 12;

    /// <summary>
    /// Zákon č. 262/2006 Sb., zákoník práce — § 90 odst. 1
    /// Minimální odpočinek mezi koncem a začátkem směny činí alespoň 11 hodin.
    /// </summary>
    public const decimal MinRestBetweenShiftsHours = 11;

    /// <summary>
    /// Zákon č. 262/2006 Sb., zákoník práce — § 92 odst. 1
    /// Nepřetržitý odpočinek v týdnu musí činit alespoň 35 hodin.
    /// 24 h týdenního + 11 h denního.
    /// </summary>
    public const decimal MinWeeklyRestHours = 35;

    /// <summary>
    /// Zákon č. 262/2006 Sb., zákoník práce - § 83 odst. 1
    /// Stanovená týdenní pracovní doba je 40 hodin (při plném úvazku).
    /// </summary>
    public const decimal StandardWeeklyWorkHours = 40;

    /// <summary>
    /// Zákon č. 262/2006 Sb., zákoník práce — § 79 odst. 1
    /// Standardní denní pracovní doba činí 8 hodin (při úvazku 1,0).
    /// </summary>
    public const decimal StandardWorkdayHours = 8;

    /// <summary>
    /// Zákon č. 262/2006 Sb., zákoník práce — § 88 odst. 1 
    /// Minimální délka přestávky na jídlo a oddech činí 30 minut.
    /// </summary>
    public const decimal MinBreakDurationHours = 0.5m;

}

public interface ITimesheetReviewer<T> where T : ITimesheet
{
    TimesheetReview Review(T timesheet);
}

public sealed class CombinedTimesheetReviewer : ITimesheetReviewer<CombinedTimesheet>
{
    public TimesheetReview Review(CombinedTimesheet timesheet)
    {
        return new TimesheetReview
        {
            Issues = ReviewTimesheet(timesheet),
            DayIssues = timesheet.Days.SelectMany(ReviewDay)
        };
    }

    private static IEnumerable<TimesheetIssue> ReviewTimesheet(CombinedTimesheet timesheet) =>
    [
        .. ReviewOvertime(timesheet),
        .. ReviewUndertime(timesheet),
        .. ReviewWeeklyWorkHours(timesheet)
    ];

    private static IEnumerable<DayIssue> ReviewDay(CombinedDay day) =>
    [
        .. ReviewOvertime(day),
        .. ReviewUndertime(day),
        .. ReviewTooLongWorkday(day),
        .. ReviewWorkOnFreeDay(day)
    ];

    private static IEnumerable<TimesheetIssue> ReviewOvertime(CombinedTimesheet timesheet)
    {
        if (timesheet.TotalHours > timesheet.TotalHoursObligation)
        {
            yield return new TimesheetIssue
            (
                Code: "ERR-COM-02",
                Type: IssueType.Error,
                Description: "Celková pracovní doba za měsíc přesahuje součet denních povinností."
            );
        }
    }

    private static IEnumerable<TimesheetIssue> ReviewUndertime(CombinedTimesheet timesheet)
    {
        if (timesheet.TotalHours < timesheet.TotalHoursObligation)
        {
            yield return new TimesheetIssue
            (
                Code: "ERR-COM-03",
                Type: IssueType.Error,
                Description: "Celková pracovní doba za měsíc je nižší než součet denních povinností."
            );
        }
    }

    private static IEnumerable<TimesheetIssue> ReviewWeeklyWorkHours(CombinedTimesheet timesheet)
    {
        List<CombinedDay> orderedWorkDays = timesheet.Days
            .Where(d => d.IsWorkday)
            .OrderBy(d => d.Date)
            .ToList();

        decimal weeklyLimit = TimesheetLimits.StandardWeeklyWorkHours * timesheet.TotalWorkload;

        var weeks = orderedWorkDays.GroupBy(d => ISOWeek.GetWeekOfYear(d.Date.ToDateTime(TimeOnly.MinValue)));
        foreach (var week in weeks)
        {
            decimal weekTotalHours = week.Sum(day => day.TotalHours);
            if (weekTotalHours > weeklyLimit)
            {
                yield return new TimesheetIssue(
                    Code: "ERR-COM-04",
                    Type: IssueType.Error,
                    Description:
                        $"V týdnu {week.Key} bylo odpracováno {weekTotalHours:F1} h, " +
                        $"což překračuje zákonný limit {weeklyLimit:F1} h při celkovém úvazku {timesheet.TotalWorkload:P0}."
                );
            }
        }
    }

    private static IEnumerable<DayIssue> ReviewOvertime(CombinedDay day)
    {
        if (day.IsWorkday && day.TotalHours > day.TotalHoursObligation)
        {
            yield return new DayIssue(
                Code: "WAR-ATT-02A",
                Type: IssueType.Warning,
                Description: "Odpracovaný čas za den je vyšší než denní pracovní povinnost.",
                Day: day.Date.Day,
                Field: nameof(day.TotalHours)
            );
        }
    }

    private static IEnumerable<DayIssue> ReviewUndertime(CombinedDay day)
    {
        if (day.IsWorkday && day.TotalHours < day.TotalHoursObligation)
        {
            yield return new DayIssue(
                Code: "WAR-ATT-02B",
                Type: IssueType.Warning,
                Description: "Odpracovaný čas za den je nižší než denní pracovní povinnost.",
                Day: day.Date.Day,
                Field: nameof(day.TotalHours)
            );
        }
    }

    private static IEnumerable<DayIssue> ReviewTooLongWorkday(CombinedDay day)
    {
        if (day.IsWorkday && day.TotalHours is > TimesheetLimits.MaxWorkShiftHours)
        {
            yield return new DayIssue
            (
                Code: "ERR-ATT-05",
                Type: IssueType.Error,
                Description: $"Odpracovaný čas za den překračuje {TimesheetLimits.MaxWorkShiftHours} hodin.",
                Day: day.Date.Day,
                Field: nameof(day.TotalHours)
            );
        }
    }

    private static IEnumerable<DayIssue> ReviewWorkOnFreeDay(CombinedDay day)
    {
        bool noObligation = day.TotalHoursObligation == 0m;
        bool worked = day.TotalHours > 0m;

        if (noObligation && worked)
        {
            yield return new DayIssue(
                Code: "WAR-COM-01",
                Type: IssueType.Warning,
                Description: "Evidována práce ve dni, kdy není uvedena žádná pracovní povinnost.",
                Day: day.Date.Day,
                Field: nameof(day.TotalHours)
            );
        }
    }
}

public sealed class AttendanceTimesheetReviewer : ITimesheetReviewer<AttendanceTimesheet>
{
    public TimesheetReview Review(AttendanceTimesheet timesheet)
    {
        return new TimesheetReview
        {
            Issues = ReviewTimesheet(timesheet),
            DayIssues = timesheet.Days.SelectMany(ReviewDay)
        };
    }

    private static IEnumerable<TimesheetIssue> ReviewTimesheet(AttendanceTimesheet timesheet) =>
    [
        .. ReviewRestBetweenWorkDays(timesheet),
    ];

    private static IEnumerable<DayIssue> ReviewDay(AttendanceDay day) =>
    [
        .. ReviewNightShift(day),
        .. ReviewBreakWithoutClockTimes(day),
        .. ReviewDayHoursObligation(day),
        .. ReviewClockOutBeforeClockIn(day),
        .. ReviewMissingClockIn(day),
        .. ReviewMissingClockOut(day),
        .. ReviewMissingBreak(day),
        .. ReviewLateBreak(day),
        .. ReviewShortBreak(day)
    ];

    private static IEnumerable<TimesheetIssue> ReviewRestBetweenWorkDays(AttendanceTimesheet timesheet)
    {
        List<AttendanceDay> orderedDays = timesheet.Days
            .Where(day => day.IsWorkday && day.ClockIn is not null && day.ClockOut is not null)
            .OrderBy(day => day.Date)
            .ToList();

        for (int i = 1; i < orderedDays.Count; i++)
        {
            AttendanceDay previous = orderedDays[i - 1];
            AttendanceDay current = orderedDays[i];

            // Přeskočit, pokud dny nejsou po sobě (např. je mezi nimi víkend/svátek/volno)
            if ((current.Date.DayNumber - previous.Date.DayNumber) > 1)
            {
                continue;
            }

            DateTime previousEnd = previous.Date.ToDateTime(previous.ClockOut!.Value);
            DateTime currentStart = current.Date.ToDateTime(current.ClockIn!.Value);

            // Korekce přes půlnoc, pro případy jako je tento:
            // previousEnd = 2024-10-01 02:00
            // currentStart = 2024-10-02 10:00
            // V tomto případě jde o to, že ve 02:00 ráno už byl další den, tedy 2024-10-02.
            if (currentStart < previousEnd)
            {
                currentStart = currentStart.AddDays(1);
            }

            decimal restHours = (decimal)(currentStart - previousEnd).TotalHours;
            if (restHours < TimesheetLimits.MinRestBetweenShiftsHours)
            {
                yield return new TimesheetIssue(
                    Code: "ERR-COM-05",
                    Type: IssueType.Error,
                    Description: $"Mezi dny {previous.Date:dd.MM.} ({previous.ClockOut:HH\\:mm}) a {current.Date:dd.MM.} ({current.ClockIn:HH\\:mm}) není zajištěn minimální odpočinek {TimesheetLimits.MinRestBetweenShiftsHours} hodin (pouze {restHours:F1} h)."
                );
            }
        }
    }

    private static IEnumerable<DayIssue> ReviewNightShift(AttendanceDay day)
    {
        TimeOnly nightStart = new(hour: 22, minute: 0);
        TimeOnly nightEnd = new(hour: 5, minute: 59);

        bool clockInStartsAtNight = day.ClockIn >= nightStart || day.ClockIn <= nightEnd;
        bool clockOutEndsAtNight = day.ClockOut >= nightStart || day.ClockOut <= nightEnd;

        if (day.IsWorkday && (day.ClockIn is not null || day.ClockOut is not null) && (clockInStartsAtNight || clockOutEndsAtNight))
        {
            yield return new DayIssue
            (
                Code: "WAR-ATT-04",
                Type: IssueType.Warning,
                Description: "Pracovní doba spadá do nočního intervalu (22:00 – 05:59).",
                Day: day.Date.Day,
                Field: nameof(day.ClockIn)
            );
        }
    }

    private static IEnumerable<DayIssue> ReviewBreakWithoutClockTimes(AttendanceDay day)
    {
        bool hasBreak = day.BreakStart is not null || day.BreakEnd is not null;
        bool missingClockBoundary = day.ClockIn is null || day.ClockOut is null;

        if (day.IsWorkday && hasBreak && missingClockBoundary)
        {
            yield return new DayIssue
            (
                Code: "WAR-ATT-05",
                Type: IssueType.Warning,
                Description: "Zadána přestávka, ale chybí příchod nebo odchod.",
                Day: day.Date.Day,
                Field: nameof(day.BreakStart)
            );
        }
    }

    private static IEnumerable<DayIssue> ReviewDayHoursObligation(AttendanceDay day)
    {
        if (day.IsWorkday && day.TotalHoursObligation is 0)
        {
            yield return new DayIssue
            (
                Code: "ERR-ATT-01",
                Type: IssueType.Error,
                Description: "Není uvedena denní pracovní povinnost pro pracovní den.",
                Day: day.Date.Day,
                Field: nameof(day.TotalHoursObligation)
            );
        }
    }

    private static IEnumerable<DayIssue> ReviewClockOutBeforeClockIn(AttendanceDay day)
    {
        if (day.IsWorkday && day.ClockOut <= day.ClockIn)
        {
            yield return new DayIssue
            (
                Code: "ERR-ATT-02",
                Type: IssueType.Error,
                Description: "Čas odchodu je dřívější nebo stejný jako příchod.",
                Day: day.Date.Day,
                Field: nameof(day.ClockOut)
            );
        }
    }

    private static IEnumerable<DayIssue> ReviewMissingClockIn(AttendanceDay day)
    {
        if (day.IsWorkday && day.ClockIn is null)
        {
            yield return new DayIssue
            (
                Code: "ERR-ATT-03",
                Type: IssueType.Error,
                Description: "Není vyplněn čas příchodu.",
                Day: day.Date.Day,
                Field: nameof(day.ClockIn)
            );
        }
    }

    private static IEnumerable<DayIssue> ReviewMissingClockOut(AttendanceDay day)
    {
        if (day.IsWorkday && day.ClockOut is null)
        {
            yield return new DayIssue
            (
                Code: "ERR-ATT-04",
                Type: IssueType.Error,
                Description: "Není vyplněn čas odchodu.",
                Day: day.Date.Day,
                Field: nameof(day.ClockOut)
            );
        }
    }

    private static IEnumerable<DayIssue> ReviewMissingBreak(AttendanceDay day)
    {
        if (day.IsWorkday && day.ClockIn is not null && day.ClockOut is not null)
        {
            if (day.TotalHours > TimesheetLimits.MaxContinuousWorkBeforeBreakHours && day.BreakStart is null)
            {
                yield return new DayIssue(
                    Code: "ERR-ATT-06",
                    Type: IssueType.Error,
                    Description: $"Chybí povinná přestávka po nejdéle {TimesheetLimits.MaxContinuousWorkBeforeBreakHours} hodinách práce.",
                    Day: day.Date.Day,
                    Field: nameof(day.BreakStart)
                );
            }
        }
    }

    private static IEnumerable<DayIssue> ReviewLateBreak(AttendanceDay day)
    {
        if (day.IsWorkday && day.ClockIn is not null && day.BreakStart is not null)
        {
            decimal hoursWorkedBeforeBreak = (decimal)(day.BreakStart.Value - day.ClockIn.Value).TotalHours;
            if (hoursWorkedBeforeBreak > TimesheetLimits.MaxContinuousWorkBeforeBreakHours)
            {
                yield return new DayIssue(
                    Code: "ERR-ATT-07",
                    Type: IssueType.Error,
                    Description: $"Přestávka začíná až po {TimesheetLimits.MaxContinuousWorkBeforeBreakHours:F1} hodinách, což překračuje zákonný limit {TimesheetLimits.MaxContinuousWorkBeforeBreakHours} h.",
                    Day: day.Date.Day,
                    Field: nameof(day.BreakStart)
                );
            }
        }
    }

    private static IEnumerable<DayIssue> ReviewShortBreak(AttendanceDay day)
    {
        if (day.IsWorkday && day.BreakStart is not null && day.BreakEnd is not null)
        {
            decimal breakDuration = (decimal)(day.BreakEnd.Value - day.BreakStart.Value).TotalHours;
            if (breakDuration < TimesheetLimits.MinBreakDurationHours)
            {
                yield return new DayIssue(
                    Code: "ERR-ATT-08",
                    Type: IssueType.Error,
                    Description: $"Délka přestávky musí být alepsoň {(TimesheetLimits.MinBreakDurationHours * 60):F0} minut.",
                    Day: day.Date.Day,
                    Field: nameof(day.BreakEnd)
                );
            }
        }
    }
}

public sealed class ProjectTimesheetReviewer : ITimesheetReviewer<ProjectTimesheet>
{
    public TimesheetReview Review(ProjectTimesheet timesheet)
    {
        throw new NotImplementedException();
    }
}

public sealed class ImportTimesheetReviewer : ITimesheetReviewer<ITimesheet<IDay>>
{
    public TimesheetReview Review(ITimesheet<IDay> timesheet)
    {
        return new TimesheetReview
        {
            Issues = ReviewTimesheet(timesheet)
        };
    }

    private static IEnumerable<TimesheetIssue> ReviewTimesheet(ITimesheet<IDay> timesheet) =>
    [
        .. ReviewDaysCount(timesheet),
        .. ReviewWorkload(timesheet)
    ];

    private static IEnumerable<TimesheetIssue> ReviewDaysCount(ITimesheet<IDay> timesheet)
    {
        int expectedDays = DateTime.DaysInMonth(timesheet.Year, timesheet.Month);
        int actualDays = timesheet.Days.Count;

        if (actualDays != expectedDays)
        {
            yield return new TimesheetIssue
            (
                Code: "",
                Type: IssueType.Error,
                Description: $"Počet dnů ve výkazu neodpovídá období výkazu {timesheet.Month}/{timesheet.Year}. Bylo nalezeno {actualDays} z očekávaných {expectedDays} dnů."
            );
        }
    }

    private static IEnumerable<TimesheetIssue> ReviewWorkload(ITimesheet<IDay> timesheet)
    {
        const decimal minWorkload = 0m;
        const decimal maxWorkload = 1m;

        if (timesheet.TotalWorkload < minWorkload || timesheet.TotalWorkload > maxWorkload)
        {
            yield return new TimesheetIssue(
                Code: "",
                Type: IssueType.Error,
                Description: $"Úvazek {timesheet.TotalWorkload:P0} je mimo povolený rozsah 0–100 %."
            );
        }
    }
}