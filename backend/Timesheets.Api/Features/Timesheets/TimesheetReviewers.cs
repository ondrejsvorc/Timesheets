namespace Timesheets.Api.Features.Timesheets;

public enum IssueType { Warning = 0, Error = 1 }

public sealed record TimesheetIssue(string Code, IssueType Type, string Description);
public sealed record DayIssue(string Code, IssueType Type, string Description, int Day, string Field);

public sealed class TimesheetReview
{
    public bool HasErrors => Issues.Any(issue => issue.Type is IssueType.Error) || DayIssues.Any(issue => issue.Type is IssueType.Error);
    public IEnumerable<TimesheetIssue> Issues { get; init; } = [];
    public IEnumerable<DayIssue> DayIssues { get; init; } = [];
}

file static class TimesheetLimits
{
    /// <summary>
    /// Zákon č. 262/2006 Sb., zákoník práce — § 88 odst. 1
    /// Přestávka nejpozději po 6 hodinách nepřetržité práce.
    /// </summary>
    public const decimal MaxContinuousWorkBeforeBreakHours = 6m;

    /// <summary>
    /// Zákon č. 262/2006 Sb., zákoník práce — § 83 odst. 4
    /// Délka směny nesmí přesáhnout 12 hodin.
    /// </summary>
    public const decimal MaxWorkShiftHours = 12m;
    public const decimal MaxNightWorkHours = 8m;

    /// <summary>
    /// Zákon č. 262/2006 Sb., zákoník práce — § 90 odst. 1
    /// Minimální odpočinek mezi koncem a začátkem směny činí alespoň 11 hodin.
    /// </summary>
    public const decimal MinRestBetweenShiftsHours = 11m;

    /// <summary>
    /// Zákon č. 262/2006 Sb., zákoník práce — § 88 odst. 1
    /// Minimální délka přestávky na jídlo a oddech činí 30 minut.
    /// </summary>
    public const decimal MinBreakDurationHours = 0.5m;
    public const decimal MinHoursBeforeBreak = 4m;
}

public sealed class CombinedTimesheetReviewer
{
    private const decimal ShortDayThresholdHours = 6m;

    public TimesheetReview Review(CombinedTimesheet timesheet, AttendanceTimesheet attendance, bool tracksAttendance)
    {
        TimesheetReview attendanceReview = tracksAttendance ? new AttendanceTimesheetReviewer().Review(attendance) : new TimesheetReview();
        return new TimesheetReview
        {
            Issues = ReviewTimesheet(timesheet).Concat(attendanceReview.Issues),
            DayIssues = timesheet.Days.SelectMany(day => ReviewDay(day, tracksAttendance)).Concat(attendanceReview.DayIssues)
        };
    }

    private static IEnumerable<TimesheetIssue> ReviewTimesheet(CombinedTimesheet timesheet) => ReviewMonthlyHours(timesheet);

    private static IEnumerable<DayIssue> ReviewDay(CombinedDay day, bool tracksAttendance) =>
    [
        .. ReviewBalance(day),
        .. ReviewStag(day, tracksAttendance),
        .. ReviewMissingAttendance(day, tracksAttendance),
        .. ReviewShortDay(day, tracksAttendance),
        .. ReviewWeekendAndHoliday(day)
    ];

    private static IEnumerable<DayIssue> ReviewShortDay(CombinedDay day, bool tracksAttendance)
    {
        if (day.SkipAllocationRules)
        {
            yield break;
        }

        if (tracksAttendance)
        {
            if (!day.HasAttendanceFilled || day.WorkedHours <= 0m || day.WorkedHours >= ShortDayThresholdHours)
            {
                yield break;
            }

            string message = $"Odpracováno jen {day.WorkedHours:F2} h (méně než 6 h).";
            yield return new DayIssue("WAR-ALL-04", IssueType.Warning, message, day.Date.Day, "clockIn");
            yield return new DayIssue("WAR-ALL-04", IssueType.Warning, message, day.Date.Day, "clockOut");
            yield break;
        }

        if (day.AllocatedHours <= 0m || day.AllocatedHours >= ShortDayThresholdHours)
        {
            yield break;
        }

        string academicMessage = $"Vykázáno jen {day.AllocatedHours:F2} h (méně než 6 h).";
        yield return new DayIssue("WAR-ALL-04", IssueType.Warning, academicMessage, day.Date.Day, "allocatedHours");
    }

    private static IEnumerable<DayIssue> ReviewBalance(CombinedDay day)
    {
        if (day.SkipAllocationRules || !day.HasAttendanceFilled || day.WorkedHours > TimesheetLimits.MaxWorkShiftHours)
        {
            yield break;
        }

        if (TimesheetLogic.HasUnequalHours(day.WorkedHours, day.AllocatedHours))
        {
            decimal balance = TimesheetLogic.Round(day.WorkedHours - day.AllocatedHours);
            if (balance > 0m)
            {
                yield return new DayIssue(
                    "ERR-ALL-01",
                    IssueType.Error,
                    $"Chybí rozdělení: docházka {day.WorkedHours:F2} h, kmen+projekty {day.AllocatedHours:F2} h.",
                    day.Date.Day,
                    "balance");
            }
            else
            {
                yield return new DayIssue(
                    "WAR-ALL-05",
                    IssueType.Warning,
                    $"Přesah rozdělení: kmen+projekty {day.AllocatedHours:F2} h, docházka {day.WorkedHours:F2} h. Upravte projektové hodiny, nebo natáhněte docházku.",
                    day.Date.Day,
                    "balance");
            }
        }
    }

    private static IEnumerable<DayIssue> ReviewStag(CombinedDay day, bool tracksAttendance)
    {
        if (!tracksAttendance && !day.SkipAllocationRules && day.CoreWorkload > 0 && day.StagHours > 0 && day.CoreHours + 0.009m < day.StagHours)
        {
            yield return new DayIssue("ERR-ALL-02", IssueType.Error, $"STAG: v kmeni musí být alespoň {day.StagHours:F2} h.", day.Date.Day, "coreHours");
        }
    }

    private static IEnumerable<DayIssue> ReviewMissingAttendance(CombinedDay day, bool tracksAttendance)
    {
        if (!tracksAttendance || day.SkipAllocationRules || day.HasAttendanceFilled)
        {
            yield break;
        }

        if (day.AllocatedHours <= 0m)
        {
            yield break;
        }

        const string message = "Doplňte docházku.";
        yield return new DayIssue("ERR-ATT-13", IssueType.Error, message, day.Date.Day, "clockIn");
        yield return new DayIssue("ERR-ATT-13", IssueType.Error, message, day.Date.Day, "clockOut");
        yield return new DayIssue("ERR-ATT-13", IssueType.Error, message, day.Date.Day, "breakStart");
        yield return new DayIssue("ERR-ATT-13", IssueType.Error, message, day.Date.Day, "breakEnd");
    }

    private static IEnumerable<TimesheetIssue> ReviewMonthlyHours(CombinedTimesheet timesheet)
    {
        if (timesheet.TotalHours + 0.009m < timesheet.TotalHoursObligation)
        {
            yield return new TimesheetIssue("ERR-COM-03", IssueType.Error, "Celková pracovní doba za měsíc je nižší než pracovní povinnost.");
        }
    }

    private static IEnumerable<DayIssue> ReviewWeekendAndHoliday(CombinedDay day)
    {
        if (day.IsWeekend && day.TotalHours > 0)
        {
            yield return new DayIssue("WAR-COM-01", IssueType.Warning, "Práce o víkendu (očekává se kompenzace v jiném pracovním dni).", day.Date.Day, "workedHours");
        }
        else if (day.IsHoliday && day.HasAttendanceFilled && day.WorkedHours > 0)
        {
            yield return new DayIssue("WAR-COM-02", IssueType.Warning, "Práce ve svátek (očekává se kompenzace v jiném pracovním dni).", day.Date.Day, "workedHours");
        }
    }
}

public sealed class AttendanceTimesheetReviewer
{
    public TimesheetReview Review(AttendanceTimesheet timesheet) => new()
    {
        Issues = ReviewTimesheet(timesheet),
        DayIssues = timesheet.Days.SelectMany(ReviewDay)
    };

    private static IEnumerable<TimesheetIssue> ReviewTimesheet(AttendanceTimesheet timesheet) =>
    [
        .. ReviewDaysCount(timesheet),
        .. ReviewRest(timesheet)
    ];

    private static IEnumerable<TimesheetIssue> ReviewDaysCount(AttendanceTimesheet timesheet)
    {
        if (timesheet.Days.Count != DateTime.DaysInMonth(timesheet.Year, timesheet.Month))
        {
            yield return new TimesheetIssue("ERR-COM-01", IssueType.Error, "Chybí některé dny v měsíci.");
        }
    }

    private static IEnumerable<TimesheetIssue> ReviewRest(AttendanceTimesheet timesheet)
    {
        List<AttendanceDay> days = timesheet.Days
            .Where(day => day.IsWorkday && day.ClockIn is not null && day.ClockOut is not null)
            .OrderBy(day => day.Date)
            .ToList();

        for (int index = 1; index < days.Count; index++)
        {
            AttendanceDay previous = days[index - 1];
            AttendanceDay current = days[index];
            if ((current.Date.Date - previous.Date.Date).Days > 1)
            {
                continue;
            }

            DateTime previousEnd = previous.Date.Date + previous.ClockOut!.Value;
            if (previous.ClockOut < previous.ClockIn)
            {
                previousEnd = previousEnd.AddDays(1);
            }

            DateTime currentStart = current.Date.Date + current.ClockIn!.Value;
            decimal rest = (decimal)(currentStart - previousEnd).TotalHours;
            if (rest < TimesheetLimits.MinRestBetweenShiftsHours)
            {
                yield return new TimesheetIssue("ERR-COM-05", IssueType.Error, $"Odpočinek mezi {previous.Date:dd.MM.} a {current.Date:dd.MM.} je jen {rest:F1} h (min. 11 h).");
            }
        }
    }

    private static IEnumerable<DayIssue> ReviewDay(AttendanceDay day)
    {
        bool activity = day.ClockIn is not null || day.ClockOut is not null || day.BreakStart is not null || day.BreakEnd is not null;
        bool hasBreak = day.BreakStart is not null || day.BreakEnd is not null;
        decimal shiftHours = TimesheetLogic.CalculateElapsedHours(day.ClockIn, day.ClockOut);
        decimal workedHours = TimesheetLogic.CalculateWorkedHoursFromAttendance(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd);

        if (day.IsWorkday && day.Workload == 0 && activity)
        {
            yield return Issue(day, "ERR-ATT-01", IssueType.Error, "Není uvedena denní pracovní povinnost.", "clockIn");
        }
        if (day.ClockIn is null && (day.ClockOut is not null || hasBreak))
        {
            yield return Issue(day, "ERR-ATT-03", IssueType.Error, "Doplňte příchod.", "clockIn");
        }
        if (day.ClockOut is null && (day.ClockIn is not null || hasBreak))
        {
            yield return Issue(day, "ERR-ATT-04", IssueType.Error, "Doplňte odchod.", "clockOut");
        }

        if (day.ClockIn is not null && day.ClockOut is not null)
        {
            bool invalidOrder = day.ClockOut == day.ClockIn || day.ClockOut < day.ClockIn && workedHours > TimesheetLimits.MaxWorkShiftHours;
            if (invalidOrder)
            {
                yield return Issue(day, "ERR-ATT-02", IssueType.Error, "Odchod musí být po příchodu.", "clockOut");
            }
            if (workedHours > TimesheetLimits.MaxWorkShiftHours)
            {
                yield return Issue(day, "ERR-ATT-05", IssueType.Error, "Práce v jednom dni přesahuje 12 h.", "clockOut");
            }
            if (TimesheetLogic.CalculateNightHours(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd) > TimesheetLimits.MaxNightWorkHours)
            {
                yield return Issue(day, "ERR-ATT-10", IssueType.Error, "Noční práce přesahuje 8 h.", "clockOut");
            }
        }

        if (day.BreakStart is not null && day.BreakEnd is null)
        {
            yield return Issue(day, "ERR-ATT-08B", IssueType.Error, "Doplňte konec přestávky.", "breakEnd");
        }
        if (day.BreakStart is null && day.BreakEnd is not null)
        {
            yield return Issue(day, "ERR-ATT-08C", IssueType.Error, "Doplňte začátek přestávky.", "breakStart");
        }
        if (hasBreak && (day.ClockIn is null || day.ClockOut is null))
        {
            yield return Issue(day, "ERR-ATT-12", IssueType.Error, "Přestávka vyžaduje příchod i odchod.", "breakStart");
        }

        if (day.ClockIn is not null && day.ClockOut is not null && day.BreakStart is not null && day.BreakEnd is not null)
        {
            decimal breakHours = TimesheetLogic.CalculateElapsedHours(day.BreakStart, day.BreakEnd);
            bool invalidBreak = day.BreakStart == day.BreakEnd || day.BreakEnd < day.BreakStart && breakHours > TimesheetLimits.MaxWorkShiftHours;
            if (invalidBreak)
            {
                yield return Issue(day, "ERR-ATT-08A", IssueType.Error, "Konec přestávky musí být po jejím začátku.", "breakEnd");
            }
            else
            {
                decimal beforeBreak = HoursBetween(day.ClockIn.Value, day.BreakStart.Value);
                if (beforeBreak < TimesheetLimits.MinHoursBeforeBreak)
                {
                    yield return Issue(day, "ERR-ATT-11", IssueType.Error, "Přestávka nejdříve po 4 h práce.", "breakStart");
                }
                if (!BreakIsInsideShift(day))
                {
                    yield return Issue(day, "ERR-ATT-09", IssueType.Error, "Přestávka musí být mezi příchodem a odchodem.", "breakStart");
                }
                if (day.IsWorkday && breakHours < TimesheetLimits.MinBreakDurationHours)
                {
                    yield return Issue(day, "ERR-ATT-08", IssueType.Error, "Přestávka musí mít alespoň 30 min.", "breakEnd");
                }
                if (shiftHours > TimesheetLimits.MaxContinuousWorkBeforeBreakHours && shiftHours <= TimesheetLimits.MaxWorkShiftHours)
                {
                    if (breakHours < TimesheetLimits.MinBreakDurationHours)
                    {
                        yield return Issue(day, "ERR-ATT-06", IssueType.Error, "Po 6 h práce je nutná přestávka alespoň 30 min.", "breakEnd");
                    }
                    else if (beforeBreak > TimesheetLimits.MaxContinuousWorkBeforeBreakHours)
                    {
                        yield return Issue(day, "ERR-ATT-07", IssueType.Error, "Přestávka musí začít nejpozději po 6 h práce.", "breakStart");
                    }
                }
            }
        }
        else if (day.ClockIn is not null && day.ClockOut is not null && shiftHours > TimesheetLimits.MaxContinuousWorkBeforeBreakHours && shiftHours <= TimesheetLimits.MaxWorkShiftHours && !hasBreak)
        {
            yield return Issue(day, "ERR-ATT-06", IssueType.Error, "Po 6 h práce je nutná přestávka alespoň 30 min.", "breakStart");
        }

        TimeSpan nightStart = new(22, 0, 0);
        TimeSpan nightEnd = new(5, 59, 0);
        bool startsAtNight = day.ClockIn >= nightStart || day.ClockIn <= nightEnd;
        bool endsAtNight = day.ClockOut >= nightStart || day.ClockOut <= nightEnd;
        if (activity && (startsAtNight || endsAtNight))
        {
            yield return Issue(day, "WAR-ATT-04", IssueType.Warning, "Práce zasahuje do noční doby (22:00–05:59).", "clockIn");
        }
    }

    private static DayIssue Issue(AttendanceDay day, string code, IssueType type, string description, string field) =>
        new(code, type, description, day.Date.Day, field);

    private static decimal HoursBetween(TimeSpan start, TimeSpan end)
    {
        decimal hours = (decimal)(end - start).TotalHours;
        return hours < 0 ? hours + 24m : hours;
    }

    private static bool BreakIsInsideShift(AttendanceDay day)
    {
        int shiftStart = (int)day.ClockIn!.Value.TotalMinutes;
        int shiftEnd = (int)day.ClockOut!.Value.TotalMinutes;
        int breakStart = (int)day.BreakStart!.Value.TotalMinutes;
        int breakEnd = (int)day.BreakEnd!.Value.TotalMinutes;

        if (shiftEnd < shiftStart)
        {
            shiftEnd += 24 * 60;
        }
        if (breakStart < shiftStart)
        {
            breakStart += 24 * 60;
        }
        if (breakEnd < breakStart)
        {
            breakEnd += 24 * 60;
        }

        return breakStart >= shiftStart && breakEnd <= shiftEnd;
    }
}
