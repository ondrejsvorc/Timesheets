
namespace Timesheets.Api.Timesheets;

public enum IssueType { Warning = 0, Error = 1 }

public sealed record TimesheetIssue(string Code, IssueType Type, string Description);
public sealed record DayIssue(string Code, IssueType Severity, string Description, int Day, string Field);

public sealed class TimesheetReview
{
    public bool HasErrors => Issues.Any(i => i.Type is IssueType.Error) || DayIssues.Any(i => i.Severity is IssueType.Error);
    public bool HasWarnings => Issues.Any(i => i.Type is IssueType.Warning) || DayIssues.Any(i => i.Severity is IssueType.Warning);

    public bool CanBeSaved => !HasErrors;
    public bool CanBeApproved => !HasErrors && !HasWarnings;

    public IEnumerable<TimesheetIssue> Issues { get; init; } = [];
    public IEnumerable<DayIssue> DayIssues { get; init; } = [];
}

public interface ITimesheetReviewer<TTimesheet>
{
    TimesheetReview Review(TTimesheet timesheet);
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
        .. ReviewDaysCount(timesheet),
        .. ReviewOvertime(timesheet),
        .. ReviewUndertime(timesheet)
    ];

    private static IEnumerable<DayIssue> ReviewDay(AttendanceDay day) =>
    [
        .. ReviewWorkOnFreeDay(day),
        .. ReviewOvertime(day),
        .. ReviewUndertime(day),
        .. ReviewNightShift(day),
        .. ReviewBreakWithoutClockTimes(day),
        .. ReviewDayHoursObligation(day),
        .. ReviewClockOutBeforeClockIn(day),
        .. ReviewMissingClockIn(day),
        .. ReviewMissingClockOut(day),
        .. ReviewTooLongWorkday(day)
    ];

    private static IEnumerable<TimesheetIssue> ReviewDaysCount(AttendanceTimesheet timesheet)
    {
        int expectedDays = DateTime.DaysInMonth(timesheet.Year, timesheet.Month);
        int actualDays = timesheet.Days.Count;

        if (actualDays != expectedDays)
        {
            yield return new TimesheetIssue
            (
                Code: "ERR-COM-01",
                Type: IssueType.Error,
                Description: "Počet záznamů v tabulce neodpovídá počtu dnů v měsíci."
            );
        }
    }

    private static IEnumerable<TimesheetIssue> ReviewOvertime(AttendanceTimesheet timesheet)
    {
        if (timesheet.TotalHoursWithoutBreak > timesheet.TotalHoursObligation)
        {
            yield return new TimesheetIssue
            (
                Code: "ERR-COM-02",
                Type: IssueType.Error,
                Description: "Celková pracovní doba za měsíc přesahuje součet denních povinností."
            );
        }
    }

    private static IEnumerable<TimesheetIssue> ReviewUndertime(AttendanceTimesheet timesheet)
    {
        if (timesheet.TotalHoursWithoutBreak < timesheet.TotalHoursObligation)
        {
            yield return new TimesheetIssue
            (
                Code: "ERR-COM-03",
                Type: IssueType.Error,
                Description: "Celková pracovní doba za měsíc je nižší než součet denních povinností."
            );
        }
    }

    private static IEnumerable<DayIssue> ReviewWorkOnFreeDay(AttendanceDay day)
    {
        bool hasClockIn = day.ClockIn is not null;
        bool hasClockOut = day.ClockOut is not null;
        bool noObligation = day.HoursObligation is 0;

        if (noObligation && (hasClockIn || hasClockOut))
        {
            yield return new DayIssue
            (
                Code: "WAR-ATT-01",
                Severity: IssueType.Warning,
                Description: "Vyplněn příchod a/nebo odchod ve dni, kdy není uvedena pracovní povinnost.",
                Day: day.Date.Day,
                Field: nameof(day.ClockIn)
            );
        }
    }

    private static IEnumerable<DayIssue> ReviewOvertime(AttendanceDay day)
    {
        if (day.IsWorkDay && day.HoursWithoutBreak > day.HoursObligation)
        {
            yield return new DayIssue(
                Code: "WAR-ATT-02A",
                Severity: IssueType.Warning,
                Description: "Odpracovaný čas za den je vyšší než denní pracovní povinnost.",
                Day: day.Date.Day,
                Field: nameof(day.HoursWithoutBreak)
            );
        }
    }

    private static IEnumerable<DayIssue> ReviewUndertime(AttendanceDay day)
    {
        if (day.IsWorkDay && day.HoursWithoutBreak < day.HoursObligation)
        {
            yield return new DayIssue(
                Code: "WAR-ATT-02B",
                Severity: IssueType.Warning,
                Description: "Odpracovaný čas za den je nižší než denní pracovní povinnost.",
                Day: day.Date.Day,
                Field: nameof(day.HoursWithoutBreak)
            );
        }
    }

    private static IEnumerable<DayIssue> ReviewNightShift(AttendanceDay day)
    {
        TimeOnly nightStart = new(hour: 22, minute: 0);
        TimeOnly nightEnd = new(hour: 5, minute: 59);

        bool clockInStartsAtNight = day.ClockIn >= nightStart || day.ClockIn <= nightEnd;
        bool clockOutEndsAtNight = day.ClockOut >= nightStart || day.ClockOut <= nightEnd;

        if (day.IsWorkDay && (day.ClockIn is not null || day.ClockOut is not null) && (clockInStartsAtNight || clockOutEndsAtNight))
        {
            yield return new DayIssue
            (
                Code: "WAR-ATT-04",
                Severity: IssueType.Warning,
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

        if (day.IsWorkDay && hasBreak && missingClockBoundary)
        {
            yield return new DayIssue
            (
                Code: "WAR-ATT-05",
                Severity: IssueType.Warning,
                Description: "Zadána přestávka, ale chybí příchod nebo odchod.",
                Day: day.Date.Day,
                Field: nameof(day.BreakStart)
            );
        }
    }

    private static IEnumerable<DayIssue> ReviewDayHoursObligation(AttendanceDay day)
    {
        if (day.IsWorkDay && day.HoursObligation is 0)
        {
            yield return new DayIssue
            (
                Code: "ERR-ATT-01",
                Severity: IssueType.Error,
                Description: "Není uvedena denní pracovní povinnost pro pracovní den.",
                Day: day.Date.Day,
                Field: nameof(day.HoursObligation)
            );
        }
    }

    private static IEnumerable<DayIssue> ReviewClockOutBeforeClockIn(AttendanceDay day)
    {
        if (day.IsWorkDay && day.ClockOut <= day.ClockIn)
        {
            yield return new DayIssue
            (
                Code: "ERR-ATT-02",
                Severity: IssueType.Error,
                Description: "Čas odchodu je dřívější nebo stejný jako příchod.",
                Day: day.Date.Day,
                Field: nameof(day.ClockOut)
            );
        }
    }

    private static IEnumerable<DayIssue> ReviewMissingClockIn(AttendanceDay day)
    {
        if (day.IsWorkDay && day.ClockIn is null)
        {
            yield return new DayIssue
            (
                Code: "ERR-ATT-03",
                Severity: IssueType.Error,
                Description: "Není vyplněn čas příchodu.",
                Day: day.Date.Day,
                Field: nameof(day.ClockIn)
            );
        }
    }

    private static IEnumerable<DayIssue> ReviewMissingClockOut(AttendanceDay day)
    {
        if (day.IsWorkDay && day.ClockOut is null)
        {
            yield return new DayIssue
            (
                Code: "ERR-ATT-04",
                Severity: IssueType.Error,
                Description: "Není vyplněn čas odchodu.",
                Day: day.Date.Day,
                Field: nameof(day.ClockOut)
            );
        }
    }

    private static IEnumerable<DayIssue> ReviewTooLongWorkday(AttendanceDay day)
    {
        if (day.IsWorkDay && day.HoursWithoutBreak is > 12)
        {
            yield return new DayIssue
            (
                Code: "ERR-ATT-05",
                Severity: IssueType.Error,
                Description: "Odpracovaný čas za den překračuje 12 hodin.",
                Day: day.Date.Day,
                Field: nameof(day.HoursWithoutBreak)
            );
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