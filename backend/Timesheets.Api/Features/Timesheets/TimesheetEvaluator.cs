using System.Text.Json;
using Timesheets.Api.Features.Employees;
using Timesheets.Api.Features.Timesheets.Allocation;

namespace Timesheets.Api.Features.Timesheets;

public sealed record TimeRange(TimeSpan Start, TimeSpan End);

public sealed record EvaluatedTimesheet(int Year, int Month, decimal CoreWorkload, IReadOnlyList<EvaluatedDay> Days)
{
    public decimal TotalHours => Days.Sum(d => d.TotalHours);
    public decimal TotalWorkload => Days.FirstOrDefault()?.TotalWorkload ?? 0m;
    public decimal TotalHoursObligation => Days.Sum(day => day.TotalHoursObligation);
}

public sealed record EvaluatedDay(DateTime Date, bool IsHoliday, decimal Workload, decimal CoreWorkload, decimal WorkedHours, decimal CoreHours, decimal ContractPartHours, decimal StagHours, bool HasAttendanceFilled, bool SkipAllocationRules)
{
    public bool IsWeekend => TimesheetEvaluator.IsWeekend(Date);
    public bool IsWorkday => TimesheetEvaluator.IsWorkday(Date, IsHoliday);
    public decimal TotalWorkload => Workload;
    public decimal AllocatedHours => TimesheetEvaluator.Normalize(CoreHours + ContractPartHours);
    public decimal TotalHours => HasAttendanceFilled ? WorkedHours : AllocatedHours;
    public decimal TotalHoursObligation => TimesheetEvaluator.CalculateTotalHoursObligation(Date, IsHoliday, Workload);
}

public sealed record AttendanceTimesheet(string EmployeePersonalNumber, string? EmployeeName, decimal Workload, int Year, int Month, IReadOnlyList<AttendanceDay> Days)
{
    public decimal TotalWorkload => Workload;
    public decimal TotalHours => Days.Sum(day => day.TotalHours);
    public decimal TotalHoursObligation => Days.Sum(day => day.TotalHoursObligation);
}

public sealed record AttendanceDay(DateTime Date, TimeSpan? ClockIn, TimeSpan? ClockOut, TimeSpan? BreakStart, TimeSpan? BreakEnd, string? OtherInterruption, IReadOnlyList<TimeRange> Schedules, bool IsHoliday, decimal Workload)
{
    public bool IsWeekend => TimesheetEvaluator.IsWeekend(Date);
    public bool IsWorkday => TimesheetEvaluator.IsWorkday(Date, IsHoliday);

    public decimal TotalWorkload => Workload;
    public decimal TotalHoursObligation => TimesheetEvaluator.CalculateTotalHoursObligation(Date, IsHoliday, Workload);
    public decimal TotalHours => TimesheetEvaluator.CalculateWorkedHoursFromAttendance(ClockIn, ClockOut, BreakStart, BreakEnd, OtherInterruption, Workload);
}

public sealed record ContractPartTimesheet(int Year, int Month, decimal Workload, IReadOnlyList<ContractPartTimesheetDay> Days)
{
    public decimal TotalWorkload => Workload;
    public decimal TotalHours => Days.Sum(day => day.TotalHours);
    public decimal TotalHoursObligation => Days.Sum(day => day.TotalHoursObligation);
}

public sealed record ContractPartTimesheetDay(DateTime Date, decimal Hours, bool IsHoliday, decimal Workload)
{
    public bool IsWeekend => TimesheetEvaluator.IsWeekend(Date);
    public bool IsWorkday => TimesheetEvaluator.IsWorkday(Date, IsHoliday);

    public decimal TotalWorkload => Workload;
    public decimal TotalHoursObligation => TimesheetEvaluator.CalculateTotalHoursObligation(Date, IsHoliday, Workload);
    public decimal TotalHours => Hours;
}

public enum IssueType { Warning = 0, Error = 1 }

public sealed record TimesheetIssue(string Code, IssueType Type, string Description);
public sealed record DayIssue(string Code, IssueType Type, string Description, int Day, string Field);

public sealed record TimesheetDayEvaluation(
    int Day,
    decimal WorkedHours,
    decimal NightHours,
    decimal AllocatedHours,
    decimal Balance,
    decimal DisplayBalance,
    bool CanAllocate,
    bool CanGenerateAttendance,
    bool CoreLocked,
    bool HasBusinessTrip,
    bool HasCoreOnlyInterruption,
    bool HasProportionalInterruption);

public sealed record ContractPartTotal(Guid ContractEmployeeId, decimal Hours, decimal Obligation, bool MatchesObligation);

public sealed record TimesheetTotals(
    decimal WorkedHours,
    decimal HoursObligation,
    decimal AllocatedHours,
    decimal CoreHours,
    decimal CoreHoursObligation,
    bool WorkedHoursMeetsObligation,
    bool CoreHoursWithinTolerance,
    IReadOnlyList<ContractPartTotal> ContractParts);

public sealed record TimesheetEvaluation(bool HasErrors, IReadOnlyList<TimesheetIssue> Issues, IReadOnlyList<DayIssue> DayIssues, IReadOnlyList<TimesheetDayEvaluation> Days, TimesheetTotals Totals);

public sealed record ContractPartDateRange(DateTime StartDate, DateTime? EndDate)
{
    public bool Includes(DateTime date)
    {
        DateTime value = ToUtcDate(date);
        return value >= ToUtcDate(StartDate) && (!EndDate.HasValue || value <= ToUtcDate(EndDate.Value));
    }

    private static DateTime ToUtcDate(DateTime value) => value.Kind == DateTimeKind.Utc ? value.Date : DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
}

public sealed record LoadedTimesheet(Domain.Models.Timesheet Timesheet, Domain.Models.Attendance Attendance, IReadOnlyList<Domain.Models.ContractPart> ContractParts, IReadOnlyDictionary<Guid, ContractPartDateRange> ContractPartRanges, decimal TotalWorkload, decimal CoreWorkload);

public sealed record ContractPartColumn(Guid Id, decimal Workload, bool Locked, ContractPartDateRange Range)
{
    public bool IsActiveOn(DateTime date) => Range.Includes(date);
}

public sealed class EditableTimesheetDay
{
    public required DateTime Date { get; init; }
    public required TimeSpan? ClockIn { get; set; }
    public required TimeSpan? ClockOut { get; set; }
    public required TimeSpan? BreakStart { get; set; }
    public required TimeSpan? BreakEnd { get; set; }
    public required string? Description { get; init; }
    public required IReadOnlyList<TimeRange> Schedules { get; init; }
    public required bool IsHoliday { get; init; }
    public required decimal CoreHours { get; set; }
    public required bool CoreHoursFixed { get; init; }
    public required Dictionary<Guid, decimal> ContractPartHours { get; init; }
    public required Dictionary<Guid, bool> ContractPartHoursFixed { get; init; }
    public required Dictionary<Guid, decimal> ContractPartHoursFloor { get; init; }

    public bool AttendanceAdjusted { get; set; }
}

public sealed record EditableTimesheet(IReadOnlyList<EditableTimesheetDay> Days, IReadOnlyList<ContractPartColumn> ContractParts);

public sealed class TimesheetReview
{
    public bool HasErrors => Issues.Any(issue => issue.Type is IssueType.Error) || DayIssues.Any(issue => issue.Type is IssueType.Error);
    public IEnumerable<TimesheetIssue> Issues { get; init; } = [];
    public IEnumerable<DayIssue> DayIssues { get; init; } = [];
}

public sealed class TimesheetEvaluator
{
    private const decimal StandardWorkdayHours = 8m;

    public TimesheetEvaluation Evaluate(LoadedTimesheet loaded, TimesheetEdit edit) => Evaluate(loaded, BuildEditableTimesheet(loaded, edit));

    public TimesheetEvaluation Evaluate(LoadedTimesheet loaded, EditableTimesheet sheet)
    {
        bool tracksAttendance = EmployeeTypes.TracksAttendance(loaded.Attendance.EmployeeTypeId);
        foreach (EditableTimesheetDay day in sheet.Days)
        {
            ApplyInterruptionToDayState(day, sheet.ContractParts, loaded.TotalWorkload, tracksAttendance);
        }

        List<AttendanceDay> attendanceDays = sheet.Days.Select(day => new AttendanceDay(Date: day.Date, ClockIn: day.ClockIn, ClockOut: day.ClockOut, BreakStart: day.BreakStart, BreakEnd: day.BreakEnd, OtherInterruption: day.Description, Schedules: day.Schedules, IsHoliday: day.IsHoliday, Workload: loaded.TotalWorkload)).ToList();
        AttendanceTimesheet attendance = new(EmployeePersonalNumber: loaded.Timesheet.Employee.PersonalNumber, EmployeeName: loaded.Timesheet.Employee.DisplayName, Workload: loaded.TotalWorkload, Year: loaded.Timesheet.Year, Month: loaded.Timesheet.Month, Days: attendanceDays);

        List<EvaluatedDay> evaluatedDays = sheet.Days.Select(day =>
        {
            decimal worked = CalculateWorkedHoursFromAttendance(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd, day.Description, loaded.TotalWorkload);
            decimal projectHours = day.ContractPartHours.Values.Sum();
            decimal stagHours = CalculateStagHours(day.Schedules);
            bool hasAttendance = tracksAttendance && (day.ClockIn is not null || day.ClockOut is not null);
            bool skipAllocationRules = SkipAllocationRules(day.Description);
            return new EvaluatedDay(Date: day.Date, IsHoliday: day.IsHoliday, Workload: loaded.TotalWorkload, CoreWorkload: loaded.CoreWorkload, WorkedHours: worked, CoreHours: day.CoreHours, ContractPartHours: projectHours, StagHours: stagHours, HasAttendanceFilled: hasAttendance, SkipAllocationRules: skipAllocationRules);
        }).ToList();

        EvaluatedTimesheet evaluated = new(Year: loaded.Timesheet.Year, Month: loaded.Timesheet.Month, CoreWorkload: loaded.CoreWorkload, Days: evaluatedDays);
        int fundedDays = sheet.Days.Count(day => IsWorkday(day.Date, day.IsHoliday));
        List<ContractPartTotal> contractPartTotals = sheet.ContractParts.Select(project =>
        {
            decimal hours = Normalize(sheet.Days.Sum(day => day.ContractPartHours.GetValueOrDefault(project.Id)));
            decimal obligation = Normalize(sheet.Days.Count(day => IsWorkday(day.Date, day.IsHoliday) && project.IsActiveOn(day.Date)) * 8m * project.Workload);
            return new ContractPartTotal(ContractEmployeeId: project.Id, Hours: hours, Obligation: obligation, MatchesObligation: !HasUnequalHours(hours, obligation));
        }).ToList();

        decimal hoursObligation = Normalize(fundedDays * 8m * loaded.TotalWorkload);
        decimal coreHours = Normalize(sheet.Days.Sum(day => day.CoreHours));
        decimal coreHoursObligation = Normalize(hoursObligation - contractPartTotals.Sum(project => project.Obligation));
        decimal workedHours = Normalize(evaluatedDays.Sum(day => day.WorkedHours));
        decimal tolerance = AllocationDayExtensions.CoreToleranceHours;
        TimesheetTotals totals = new(
            WorkedHours: workedHours,
            HoursObligation: hoursObligation,
            AllocatedHours: Normalize(evaluatedDays.Sum(day => day.AllocatedHours)),
            CoreHours: coreHours,
            CoreHoursObligation: coreHoursObligation,
            WorkedHoursMeetsObligation: workedHours + 0.009m >= hoursObligation,
            CoreHoursWithinTolerance: coreHours + 0.009m >= coreHoursObligation - tolerance && coreHours <= coreHoursObligation + tolerance + 0.009m,
            ContractParts: contractPartTotals);

        TimesheetReview review = new EvaluatedTimesheetReviewer().Review(evaluated, attendance, tracksAttendance);
        IReadOnlyList<TimesheetIssue> issues = review.Issues.Concat(ReviewContractPartTotals(contractPartTotals)).Concat(ReviewCoreTolerance(totals)).ToArray();
        IReadOnlyList<DayIssue> dayIssues = review.DayIssues.Concat(ReviewBusinessTripInterruptions(sheet.Days)).ToArray();

        List<TimesheetDayEvaluation> days = sheet.Days.Zip(evaluatedDays).Select(pair =>
        {
            (EditableTimesheetDay day, EvaluatedDay evaluatedDay) = pair;
            bool businessTrip = HasBusinessTripInterruption(day.Description);
            bool proportional = HasProportionalInterruption(day.Description);
            bool coreOnly = false;
            bool coreLocked = coreOnly || HasFullDayInterruption(day.Description);
            decimal balance = evaluatedDay.SkipAllocationRules || !evaluatedDay.HasAttendanceFilled ? 0m : Round(evaluatedDay.WorkedHours - evaluatedDay.AllocatedHours);
            decimal nightHours = CalculateNightHours(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd);
            IEnumerable<DayIssue> issuesForDay = dayIssues.Where(issue => issue.Day == day.Date.Day);
            decimal allocatedInputHours = Normalize(day.CoreHours + day.ContractPartHours.Values.Sum());
            bool canGenerateAttendance = tracksAttendance
                && issuesForDay.Any(issue => issue.Code == "ERR-ATT-13")
                && (day.Schedules.Count > 0 || allocatedInputHours > 0m);
            decimal stagMissing = !tracksAttendance && issuesForDay.Any(issue => issue.Code == "ERR-ALL-02")
                ? Math.Max(0m, Round(evaluatedDay.StagHours - day.CoreHours))
                : 0m;
            decimal displayBalance = stagMissing > 0m ? Math.Max(balance, stagMissing) : balance;
            bool canAllocate = displayBalance != 0m || canGenerateAttendance;
            return new TimesheetDayEvaluation(
                Day: day.Date.Day,
                WorkedHours: evaluatedDay.WorkedHours,
                NightHours: nightHours,
                AllocatedHours: evaluatedDay.AllocatedHours,
                Balance: balance,
                DisplayBalance: displayBalance,
                CanAllocate: canAllocate,
                CanGenerateAttendance: canGenerateAttendance,
                CoreLocked: coreLocked,
                HasBusinessTrip: businessTrip,
                HasCoreOnlyInterruption: coreOnly,
                HasProportionalInterruption: proportional);
        }).ToList();

        return new TimesheetEvaluation(HasErrors: issues.Any(issue => issue.Type is IssueType.Error) || dayIssues.Any(issue => issue.Type is IssueType.Error), Issues: issues, DayIssues: dayIssues, Days: days, Totals: totals);
    }

    public EditableTimesheet BuildEditableTimesheet(LoadedTimesheet loaded, TimesheetEdit edit)
    {
        Dictionary<DateOnly, DayEdit> days = edit.Days.ToDictionary(day => DateOnly.FromDateTime(day.Date));
        Dictionary<Guid, ContractPartEdit> projects = (edit.ContractParts ?? []).ToDictionary(project => project.ContractEmployeeId);
        List<ContractPartColumn> contractPartStates = ContractPartColumns(loaded);
        Dictionary<Guid, ContractPartColumn> contractPartStatesById = contractPartStates.ToDictionary(project => project.Id);

        List<EditableTimesheetDay> dayStates = loaded.Attendance.Days
            .OrderBy(day => day.Date)
            .Select(day =>
            {
                DateOnly date = DateOnly.FromDateTime(day.Date);
                DayEdit? update = days.GetValueOrDefault(date);
                Dictionary<Guid, decimal> projectHours = [];
                Dictionary<Guid, bool> projectHoursFixed = [];
                Dictionary<Guid, decimal> projectHoursFloor = [];
                string? description = update is null ? day.Description : update.Description;
                bool editableHalfDayInterruption = HasEditableHalfDayInterruption(description);

                foreach (Domain.Models.ContractPart project in loaded.ContractParts)
                {
                    ContractPartColumn projectState = contractPartStatesById[project.ContractEmployeeId];
                    ContractPartEdit? contractPartUpdate = projects.GetValueOrDefault(project.ContractEmployeeId);
                    if (project.LockedAt is not null || !projectState.IsActiveOn(day.Date))
                    {
                        contractPartUpdate = null;
                    }

                    Domain.Models.ContractPartDay? persistedDay = projectState.IsActiveOn(day.Date)
                        ? project.Days.FirstOrDefault(contractPartDay => DateOnly.FromDateTime(contractPartDay.Date) == date)
                        : null;
                    decimal persisted = persistedDay?.Hours ?? 0m;
                    ContractPartDayEdit? contractPartDayUpdate = contractPartUpdate?.Days.FirstOrDefault(contractPartDay => DateOnly.FromDateTime(contractPartDay.Date) == date);
                    decimal hours = contractPartDayUpdate?.Hours ?? persisted;
                    projectHours[project.ContractEmployeeId] = Normalize(hours);
                    projectHoursFixed[project.ContractEmployeeId] = !editableHalfDayInterruption && projectState.IsActiveOn(day.Date) && (contractPartDayUpdate?.HoursLocked ?? persistedDay?.HoursLocked ?? false);
                    bool projectFixed = projectHoursFixed[project.ContractEmployeeId];
                    projectHoursFloor[project.ContractEmployeeId] = projectFixed && hours > 0m ? Normalize(hours) : 0m;
                }

                return new EditableTimesheetDay
                {
                    Date = day.Date,
                    ClockIn = update is null ? day.ClockIn : update.ClockIn,
                    ClockOut = update is null ? day.ClockOut : update.ClockOut,
                    BreakStart = update is null ? day.BreakStart : update.BreakStart,
                    BreakEnd = update is null ? day.BreakEnd : update.BreakEnd,
                    Description = description,
                    Schedules = update is null ? ParseSchedules(day.Schedules) : update.Schedules ?? [],
                    IsHoliday = day.IsHoliday,
                    CoreHours = Normalize(update is null ? day.CoreHours : update.CoreHours),
                    CoreHoursFixed = !editableHalfDayInterruption && (update?.CoreHoursFixed ?? false),
                    ContractPartHours = projectHours,
                    ContractPartHoursFixed = projectHoursFixed,
                    ContractPartHoursFloor = projectHoursFloor
                };
            })
            .ToList();

        return new EditableTimesheet(Days: dayStates, ContractParts: contractPartStates);
    }

    public TimesheetEdit CurrentEdit(LoadedTimesheet loaded)
    {
        DayEdit[] days = loaded.Attendance.Days.Select(day => new DayEdit(Date: day.Date, ClockIn: day.ClockIn, ClockOut: day.ClockOut, BreakStart: day.BreakStart, BreakEnd: day.BreakEnd, CoreHours: day.CoreHours, Description: day.Description, Schedules: ParseSchedules(day.Schedules))).ToArray();
        ContractPartEdit[] projects = loaded.ContractParts.Select(project =>
        {
            ContractPartDayEdit[] contractPartDays = project.Days.Select(day => new ContractPartDayEdit(Date: day.Date, Hours: day.Hours, HoursLocked: day.HoursLocked)).ToArray();
            return new ContractPartEdit(ContractEmployeeId: project.ContractEmployeeId, Days: contractPartDays);
        }).ToArray();
        return new TimesheetEdit(Days: days, ContractParts: projects);
    }

    public bool HasInactiveContractPartHours(LoadedTimesheet loaded, TimesheetEdit edit)
    {
        foreach (ContractPartEdit project in edit.ContractParts ?? [])
        {
            if (!loaded.ContractPartRanges.TryGetValue(project.ContractEmployeeId, out ContractPartDateRange? range))
            {
                continue;
            }

            if (project.Days.Any(day => !range.Includes(day.Date) && day.Hours > 0m))
            {
                return true;
            }
        }

        return false;
    }

    public static void ApplyInterruptionToDayState(EditableTimesheetDay day, IReadOnlyList<ContractPartColumn> projects, decimal totalWorkload, bool tracksAttendance)
    {
        if (HasBusinessTripInterruption(day.Description))
        {
            return;
        }

        decimal absenceHours = InterruptionAbsenceHours(day.Description, totalWorkload);
        if (HasFullDayInterruption(day.Description))
        {
            decimal capacity = DayCapacity(day.Date, day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd, day.Description, totalWorkload, tracksAttendance, day.Schedules);
            if (capacity <= 0m)
            {
                return;
            }

            ApplyProportionalInterruption(day, projects, totalWorkload, capacity);
            return;
        }

        if (HasHalfDayInterruption(day.Description))
        {
            ApplyHalfDayInterruption(day, projects, totalWorkload, absenceHours);
        }
    }

    public static bool IsWeekend(DateTime date) => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    public static bool IsWeekday(DateTime date) => !IsWeekend(date);
    public static bool IsWorkday(DateTime date, bool isHoliday) => date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday && !isHoliday;
    public static decimal CalculateTotalHoursObligation(DateTime date, bool isHoliday, decimal workload) => IsWorkday(date, isHoliday) ? Normalize(StandardWorkdayHours * workload) : 0m;

    public static decimal CalculateWorkedHoursFromAttendance(TimeSpan? clockIn, TimeSpan? clockOut, TimeSpan? breakStart, TimeSpan? breakEnd)
    {
        decimal workedHours = CalculateWorkedHours(clockIn, clockOut);
        decimal breakHours = CalculateBreakHours(breakStart, breakEnd, clockIn, clockOut);
        return Normalize(Math.Max(0, workedHours - breakHours));
    }

    public static decimal CalculateWorkedHoursFromAttendance(TimeSpan? clockIn, TimeSpan? clockOut, TimeSpan? breakStart, TimeSpan? breakEnd, string? description, decimal totalWorkload)
    {
        decimal absenceHours = InterruptionAbsenceHours(description, totalWorkload);
        if (HasFullDayInterruption(description))
        {
            return absenceHours;
        }

        return Normalize(CalculateWorkedHoursFromAttendance(clockIn, clockOut, breakStart, breakEnd) + absenceHours);
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

    public static decimal DayCapacity(DateTime date, TimeSpan? clockIn, TimeSpan? clockOut, TimeSpan? breakStart, TimeSpan? breakEnd, string? description, decimal totalWorkload, bool tracksAttendance, IReadOnlyList<TimeRange>? schedules = null)
    {
        if (tracksAttendance)
        {
            decimal worked = CalculateWorkedHoursFromAttendance(clockIn, clockOut, breakStart, breakEnd, description, totalWorkload);
            if (worked > 0m)
            {
                return Math.Min(12m, worked);
            }

            return 0m;
        }

        if (IsWeekday(date) || !string.IsNullOrWhiteSpace(description))
        {
            return Normalize(8m * totalWorkload);
        }

        decimal stagHours = schedules is { Count: > 0 } ? CalculateStagHours(schedules) : 0m;
        return stagHours > 0m ? Normalize(Math.Min(12m, stagHours)) : 0m;
    }

    public static bool HasBusinessTripInterruption(string? raw) => ParseInterruptionCodes(raw).Any(BusinessTripCodes.Contains);

    public static bool HasProportionalInterruption(string? raw)
    {
        string[] codes = ParseInterruptionCodes(raw);
        return codes.Length > 0 && !codes.Any(BusinessTripCodes.Contains);
    }

    public static bool HasHalfDayInterruption(string? raw) => HasProportionalInterruption(raw) && ParseInterruptionParts(raw).Any(HasHalfDayMarker);

    public static bool HasFullDayInterruption(string? raw)
    {
        if (!HasProportionalInterruption(raw))
        {
            return false;
        }

        string[] parts = ParseInterruptionParts(raw);
        return parts.Any(part => !HasHalfDayMarker(part)) || parts.Count(HasHalfDayMarker) >= 2;
    }

    public static bool HasEditableHalfDayInterruption(string? raw) => HasHalfDayInterruption(raw) && !HasFullDayInterruption(raw);

    public static bool SkipAllocationRules(string? raw) => HasBusinessTripInterruption(raw) || HasFullDayInterruption(raw);

    private static readonly HashSet<string> BusinessTripCodes = ["SCP", "SCS", "SCT", "SCZ", "SCZE", "SCZP", "SCZS"];

    private static decimal InterruptionAbsenceHours(string? raw, decimal totalWorkload)
    {
        if (!HasProportionalInterruption(raw))
        {
            return 0m;
        }

        if (HasFullDayInterruption(raw))
        {
            return Normalize(StandardWorkdayHours * totalWorkload);
        }

        decimal halfDays = Math.Min(1m, ParseInterruptionParts(raw).Count(HasHalfDayMarker) / 2m);
        return Normalize(StandardWorkdayHours * totalWorkload * halfDays);
    }

    private static string[] ParseInterruptionCodes(string? raw) =>
        ParseInterruptionParts(raw)
            .Select(part => part.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code!.ToUpperInvariant())
            .ToArray();

    private static string[] ParseInterruptionParts(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool HasHalfDayMarker(string value)
    {
        string upper = value.ToUpperInvariant();
        return upper.Contains("P\u016eLDEN", StringComparison.Ordinal) || upper.Contains("PULDEN", StringComparison.Ordinal);
    }

    private static void ApplyHalfDayInterruption(EditableTimesheetDay day, IReadOnlyList<ContractPartColumn> projects, decimal totalWorkload, decimal absenceHours)
    {
        if (totalWorkload <= 0m || absenceHours <= 0m)
        {
            return;
        }

        List<ContractPartColumn> activeProjects = projects.Where(project => project.IsActiveOn(day.Date)).ToList();
        decimal projectWorkload = activeProjects.Sum(project => project.Workload);
        decimal coreWorkload = Math.Max(0m, totalWorkload - projectWorkload);
        decimal distributionWorkload = Normalize(coreWorkload + projectWorkload);
        if (distributionWorkload <= 0m)
        {
            return;
        }

        decimal allocated = 0m;
        if (coreWorkload > 0m)
        {
            decimal coreFloor = Normalize(absenceHours * coreWorkload / distributionWorkload);
            day.CoreHours = Normalize(Math.Max(day.CoreHours, coreFloor));
            allocated += coreFloor;
        }

        foreach (ContractPartColumn project in projects.Where(project => !project.IsActiveOn(day.Date)))
        {
            day.ContractPartHoursFixed[project.Id] = false;
            day.ContractPartHoursFloor[project.Id] = 0m;
            day.ContractPartHours[project.Id] = 0m;
        }

        for (int index = 0; index < activeProjects.Count; index++)
        {
            ContractPartColumn project = activeProjects[index];
            decimal floor = index == activeProjects.Count - 1
                ? Normalize(Math.Max(0m, absenceHours - allocated))
                : Normalize(absenceHours * project.Workload / distributionWorkload);

            day.ContractPartHoursFixed[project.Id] = false;
            day.ContractPartHoursFloor[project.Id] = floor;
            day.ContractPartHours[project.Id] = Normalize(Math.Max(day.ContractPartHours.GetValueOrDefault(project.Id), floor));
            allocated += floor;
        }
    }

    private static void ApplyProportionalInterruption(EditableTimesheetDay day, IReadOnlyList<ContractPartColumn> projects, decimal totalWorkload, decimal capacity)
    {
        if (totalWorkload <= 0m)
        {
            return;
        }

        List<ContractPartColumn> activeProjects = projects.Where(project => project.IsActiveOn(day.Date)).ToList();
        decimal projectWorkload = activeProjects.Sum(project => project.Workload);
        decimal coreWorkload = Math.Max(0m, totalWorkload - projectWorkload);
        decimal allocated = 0m;
        if (day.CoreHoursFixed)
        {
            allocated += day.CoreHours;
        }

        foreach (ContractPartColumn project in projects.Where(project => !project.IsActiveOn(day.Date)))
        {
            if (day.ContractPartHoursFixed.GetValueOrDefault(project.Id))
            {
                allocated += day.ContractPartHours.GetValueOrDefault(project.Id);
            }
            else
            {
                day.ContractPartHours[project.Id] = day.ContractPartHoursFloor.GetValueOrDefault(project.Id);
            }
        }

        foreach (ContractPartColumn project in activeProjects.Where(project => day.ContractPartHoursFixed.GetValueOrDefault(project.Id)))
        {
            allocated += day.ContractPartHours.GetValueOrDefault(project.Id);
        }

        List<ContractPartColumn> mutableProjects = activeProjects.Where(project => !day.ContractPartHoursFixed.GetValueOrDefault(project.Id)).ToList();
        if (!day.CoreHoursFixed)
        {
            day.CoreHours = 0m;
        }
        foreach (ContractPartColumn project in mutableProjects)
        {
            day.ContractPartHours[project.Id] = day.ContractPartHoursFloor.GetValueOrDefault(project.Id);
        }

        decimal mutableWorkload = (day.CoreHoursFixed ? 0m : coreWorkload) + mutableProjects.Sum(project => project.Workload);
        decimal remaining = Normalize(Math.Max(0m, capacity - allocated));
        if (mutableWorkload <= 0m || remaining <= 0m)
        {
            return;
        }

        if (!day.CoreHoursFixed)
        {
            day.CoreHours = Normalize(remaining * coreWorkload / mutableWorkload);
            allocated += day.CoreHours;
        }

        for (int index = 0; index < mutableProjects.Count; index++)
        {
            ContractPartColumn project = mutableProjects[index];
            decimal floor = day.ContractPartHoursFloor.GetValueOrDefault(project.Id);
            decimal hours = index == mutableProjects.Count - 1
                ? Normalize(Math.Max(floor, Math.Max(0m, capacity - allocated)))
                : Normalize(Math.Max(floor, remaining * project.Workload / mutableWorkload));
            day.ContractPartHours[project.Id] = Normalize(Math.Round(hours * 2m, MidpointRounding.AwayFromZero) / 2m);
            allocated += day.ContractPartHours[project.Id];
        }
    }

    private static IEnumerable<TimesheetIssue> ReviewContractPartTotals(IReadOnlyList<ContractPartTotal> contractPartTotals)
    {
        foreach (ContractPartTotal project in contractPartTotals)
        {
            if (HasUnequalHours(project.Hours, project.Obligation))
            {
                yield return new TimesheetIssue(
                    "ERR-COM-06",
                    IssueType.Error,
                    $"Projektová část nesedí s cílem ({project.Hours:F2}/{project.Obligation:F2} h).");
            }
        }
    }

    private static IEnumerable<TimesheetIssue> ReviewCoreTolerance(TimesheetTotals totals)
    {
        decimal tolerance = AllocationDayExtensions.CoreToleranceHours;
        if (totals.CoreHours + 0.009m < totals.CoreHoursObligation - tolerance || totals.CoreHours > totals.CoreHoursObligation + tolerance + 0.009m)
        {
            yield return new TimesheetIssue(
                "WAR-COM-03",
                IssueType.Warning,
                $"Kmen se liší od cíle ({totals.CoreHours:F2}/{totals.CoreHoursObligation:F2} h, tolerance ±{tolerance:F0} h).");
        }
    }

    private static IEnumerable<DayIssue> ReviewBusinessTripInterruptions(IEnumerable<EditableTimesheetDay> days)
    {
        foreach (EditableTimesheetDay day in days.Where(day => HasBusinessTripInterruption(day.Description)))
        {
            yield return new DayIssue(
                "WAR-INT-01",
                IssueType.Warning,
                "Služební cesta vyžaduje ruční rozdělení hodin do správného úvazku.",
                day.Date.Day,
                "interruptions");
        }
    }

    private static List<ContractPartColumn> ContractPartColumns(LoadedTimesheet loaded) => loaded.ContractParts
        .Select(project => new ContractPartColumn(
            Id: project.ContractEmployeeId,
            Workload: project.Workload,
            Locked: project.LockedAt is not null,
            Range: loaded.ContractPartRanges.GetValueOrDefault(project.ContractEmployeeId) ?? new ContractPartDateRange(DateTime.MinValue, null)))
        .ToList();

    private static IReadOnlyList<TimeRange> ParseSchedules(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<TimeRange>>(value) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
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

file static class TimesheetLimits
{
    public const decimal MaxContinuousWorkBeforeBreakHours = 6m;
    public const decimal MaxWorkShiftHours = 12m;
    public const decimal MaxNightWorkHours = 8m;
    public const decimal MinRestBetweenShiftsHours = 11m;
    public const decimal MinBreakDurationHours = 0.5m;
}

public sealed class EvaluatedTimesheetReviewer
{
    private const decimal ShortDayThresholdHours = 6m;

    public TimesheetReview Review(EvaluatedTimesheet timesheet, AttendanceTimesheet attendance, bool tracksAttendance)
    {
        TimesheetReview attendanceReview = tracksAttendance ? new AttendanceTimesheetReviewer().Review(attendance) : new();
        return new TimesheetReview
        {
            Issues = ReviewTimesheet(timesheet).Concat(attendanceReview.Issues),
            DayIssues = timesheet.Days.SelectMany(day => ReviewDay(day, tracksAttendance)).Concat(attendanceReview.DayIssues)
        };
    }

    private static IEnumerable<TimesheetIssue> ReviewTimesheet(EvaluatedTimesheet timesheet) => ReviewMonthlyHours(timesheet);

    private static IEnumerable<DayIssue> ReviewDay(EvaluatedDay day, bool tracksAttendance) =>
    [
        .. ReviewMaxDailyHours(day),
        .. ReviewBalance(day),
        .. ReviewStag(day, tracksAttendance),
        .. ReviewMissingAttendance(day, tracksAttendance),
        .. ReviewShortDay(day, tracksAttendance),
        .. ReviewWeekendAndHoliday(day)
    ];

    private static IEnumerable<DayIssue> ReviewMaxDailyHours(EvaluatedDay day)
    {
        if (day.WorkedHours > TimesheetLimits.MaxWorkShiftHours)
        {
            yield return new DayIssue(
                "ERR-ALL-07",
                IssueType.Error,
                "Docházka včetně nepřítomnosti přesahuje 12 h.",
                day.Date.Day,
                "workedHours");
        }
    }

    private static IEnumerable<DayIssue> ReviewShortDay(EvaluatedDay day, bool tracksAttendance)
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

    private static IEnumerable<DayIssue> ReviewBalance(EvaluatedDay day)
    {
        if (day.SkipAllocationRules || !day.HasAttendanceFilled || day.WorkedHours > TimesheetLimits.MaxWorkShiftHours)
        {
            yield break;
        }

        if (TimesheetEvaluator.HasUnequalHours(day.WorkedHours, day.AllocatedHours))
        {
            decimal balance = TimesheetEvaluator.Round(day.WorkedHours - day.AllocatedHours);
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

    private static IEnumerable<DayIssue> ReviewStag(EvaluatedDay day, bool tracksAttendance)
    {
        if (!tracksAttendance && !day.SkipAllocationRules && day.CoreWorkload > 0 && day.StagHours > 0 && day.CoreHours + 0.009m < day.StagHours)
        {
            yield return new DayIssue("ERR-ALL-02", IssueType.Error, $"STAG: v kmeni musí být alespoň {day.StagHours:F2} h.", day.Date.Day, "coreHours");
        }
    }

    private static IEnumerable<DayIssue> ReviewMissingAttendance(EvaluatedDay day, bool tracksAttendance)
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

    private static IEnumerable<TimesheetIssue> ReviewMonthlyHours(EvaluatedTimesheet timesheet)
    {
        if (timesheet.TotalHours + 0.009m < timesheet.TotalHoursObligation)
        {
            yield return new TimesheetIssue("ERR-COM-03", IssueType.Error, "Celková pracovní doba za měsíc je nižší než pracovní povinnost.");
        }
    }

    private static IEnumerable<DayIssue> ReviewWeekendAndHoliday(EvaluatedDay day)
    {
        if (day.IsWeekend && day.TotalHours > 0)
        {
            yield return new DayIssue("WAR-COM-01", IssueType.Warning, "Práce o víkendu (očekává se kompenzace v jiném pracovním dni).", day.Date.Day, "workedHours");
        }
        else if (day.IsHoliday && day.TotalHours > 0)
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
            .Where(day => day.IsWorkday && !TimesheetEvaluator.HasFullDayInterruption(day.OtherInterruption) && day.ClockIn is not null && day.ClockOut is not null)
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
        if (TimesheetEvaluator.HasFullDayInterruption(day.OtherInterruption))
        {
            yield break;
        }

        bool activity = day.ClockIn is not null || day.ClockOut is not null || day.BreakStart is not null || day.BreakEnd is not null;
        bool hasBreak = day.BreakStart is not null || day.BreakEnd is not null;
        decimal shiftHours = TimesheetEvaluator.CalculateElapsedHours(day.ClockIn, day.ClockOut);
        decimal workedHours = TimesheetEvaluator.CalculateWorkedHoursFromAttendance(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd);

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
            if (TimesheetEvaluator.CalculateNightHours(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd) > TimesheetLimits.MaxNightWorkHours)
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
            decimal breakHours = TimesheetEvaluator.CalculateElapsedHours(day.BreakStart, day.BreakEnd);
            bool invalidBreak = day.BreakStart == day.BreakEnd || day.BreakEnd < day.BreakStart && breakHours > TimesheetLimits.MaxWorkShiftHours;
            if (invalidBreak)
            {
                yield return Issue(day, "ERR-ATT-08A", IssueType.Error, "Konec přestávky musí být po jejím začátku.", "breakEnd");
            }
            else
            {
                decimal beforeBreak = HoursBetween(day.ClockIn.Value, day.BreakStart.Value);
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
