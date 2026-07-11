using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Features.Employees;
using Timesheets.Api.Features.Timesheets.Allocation;

namespace Timesheets.Api.Features.Timesheets;

public sealed record TimesheetDayEdit(DateTime Date, TimeSpan? ClockIn, TimeSpan? ClockOut, TimeSpan? BreakStart, TimeSpan? BreakEnd, decimal CoreHours, string? Description, IReadOnlyList<TimeRange>? Schedules, bool CoreHoursFixed = false);

public sealed record ContractPartDayEdit(DateTime Date, decimal Hours, bool HoursLocked = false);

public sealed record ContractPartEdit(Guid ContractEmployeeId, IReadOnlyList<ContractPartDayEdit> Days);

public sealed record TimesheetEditRequest(IReadOnlyList<TimesheetDayEdit> Days, IReadOnlyList<ContractPartEdit>? ContractParts);

public sealed class TimesheetEditRequestValidator : AbstractValidator<TimesheetEditRequest>
{
    public TimesheetEditRequestValidator()
    {
        RuleFor(request => request.Days).NotEmpty().Must(HaveUniqueDates);
        RuleFor(request => request.ContractParts).Must(HaveUniqueContractParts);
        RuleForEach(request => request.Days).ChildRules(day =>
        {
            day.RuleFor(value => value.CoreHours).InclusiveBetween(0m, 12m);
            day.RuleFor(value => value.ClockIn).Must(IsTimeOfDay);
            day.RuleFor(value => value.ClockOut).Must(IsTimeOfDay);
            day.RuleFor(value => value.BreakStart).Must(IsTimeOfDay);
            day.RuleFor(value => value.BreakEnd).Must(IsTimeOfDay);
        });
        RuleForEach(request => request.ContractParts).ChildRules(project =>
        {
            project.RuleFor(value => value.Days).Must(HaveUniqueDates);
            project.RuleFor(value => value.Days).Must(HaveAtMostOneNonHalfHour);
            project.RuleForEach(value => value.Days).ChildRules(day =>
            {
                day.RuleFor(value => value.Hours).InclusiveBetween(0m, 12m);
            });
        });
    }

    private static bool IsHalfHourIncrement(decimal hours) => hours * 2m % 1m == 0m;
    private static bool HaveAtMostOneNonHalfHour(IEnumerable<ContractPartDayEdit> days) => days.Count(day => !IsHalfHourIncrement(day.Hours)) <= 1;

    private static bool IsTimeOfDay(TimeSpan? value) => value is null || value >= TimeSpan.Zero && value < TimeSpan.FromDays(1);
    private static bool HaveUniqueDates(IEnumerable<TimesheetDayEdit> days) => days.Select(day => DateOnly.FromDateTime(day.Date)).Distinct().Count() == days.Count();
    private static bool HaveUniqueDates(IEnumerable<ContractPartDayEdit> days) => days.Select(day => DateOnly.FromDateTime(day.Date)).Distinct().Count() == days.Count();
    private static bool HaveUniqueContractParts(IEnumerable<ContractPartEdit>? projects) => projects is null || projects.Select(project => project.ContractEmployeeId).Distinct().Count() == projects.Count();
}

public sealed record TimesheetDayEvaluation(int Day, decimal WorkedHours, decimal NightHours, decimal AllocatedHours, decimal Balance, bool HasBusinessTrip, bool HasCoreOnlyInterruption, bool HasProportionalInterruption);

public sealed record ContractPartTotal(Guid ContractEmployeeId, decimal Hours, decimal Obligation);

public sealed record TimesheetTotals(decimal WorkedHours, decimal HoursObligation, decimal AllocatedHours, decimal CoreHours, decimal CoreHoursObligation, IReadOnlyList<ContractPartTotal> ContractParts);

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

public sealed record LoadedTimesheet(Data.Models.Timesheet Timesheet, Data.Models.Attendance Attendance, IReadOnlyList<Data.Models.ContractPart> ContractParts, IReadOnlyDictionary<Guid, ContractPartDateRange> ContractPartRanges, decimal TotalWorkload, decimal CoreWorkload);

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

    /// <summary>Set when the generator raised the user's attendance to cover allocated hours.</summary>
    public bool AttendanceAdjusted { get; set; }
}

public sealed record EditableTimesheet(IReadOnlyList<EditableTimesheetDay> Days, IReadOnlyList<ContractPartColumn> ContractParts);

public static class TimesheetEngine
{
    public static async Task<LoadedTimesheet?> LoadAsync(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Data.Models.Timesheet? timesheet = await dbContext.Timesheets
            .Include(value => value.Employee)
            .Include(value => value.TimesheetStatus)
            .SingleOrDefaultAsync(value => value.Id == id, cancellationToken);

        if (timesheet is null)
        {
            return null;
        }

        Data.Models.Attendance? attendance = await dbContext.Attendances
            .Include(value => value.Days)
            .SingleOrDefaultAsync(value => value.TimesheetId == id, cancellationToken);

        if (attendance is null)
        {
            return null;
        }

        List<Data.Models.ContractPart> projects = await dbContext.ContractParts
            .Include(value => value.Days)
            .Where(value => value.TimesheetId == timesheet.Id)
            .ToListAsync(cancellationToken);

        Guid[] assignmentIds = projects.Select(project => project.ContractEmployeeId).ToArray();
        var rangeRows = await (
            from assignment in dbContext.ContractEmployees.AsNoTracking()
            join contract in dbContext.Contracts.AsNoTracking() on assignment.ContractId equals contract.Id
            join project in dbContext.Projects.AsNoTracking() on contract.ProjectId equals project.Id
            where assignmentIds.Contains(assignment.Id)
            select new
            {
                assignment.Id,
                assignment.StartDate,
                AssignmentEndDate = assignment.EndDate,
                ProjectStartDate = project.StartDate,
                ProjectEndDate = project.EndDate
            })
            .ToListAsync(cancellationToken);
        Dictionary<Guid, ContractPartDateRange> projectRanges = rangeRows.ToDictionary(
            row => row.Id,
            row => EffectiveContractPartRange(row.StartDate, row.AssignmentEndDate, row.ProjectStartDate, row.ProjectEndDate));

        decimal totalWorkload = await TimesheetWorkloads.GetAsync(timesheet.EmployeeId, timesheet.Year, timesheet.Month, dbContext, cancellationToken);
        decimal coreWorkload = Math.Max(0m, totalWorkload - projects.Sum(project => project.Workload));
        return new LoadedTimesheet(Timesheet: timesheet, Attendance: attendance, ContractParts: projects, ContractPartRanges: projectRanges, TotalWorkload: totalWorkload, CoreWorkload: coreWorkload);
    }

    public static EditableTimesheet BuildEditableTimesheet(LoadedTimesheet loaded, TimesheetEditRequest request)
    {
        Dictionary<DateOnly, TimesheetDayEdit> days = request.Days.ToDictionary(day => DateOnly.FromDateTime(day.Date));
        Dictionary<Guid, ContractPartEdit> projects = (request.ContractParts ?? []).ToDictionary(project => project.ContractEmployeeId);
        List<ContractPartColumn> contractPartStates = ContractPartColumns(loaded);
        Dictionary<Guid, ContractPartColumn> contractPartStatesById = contractPartStates.ToDictionary(project => project.Id);

        List<EditableTimesheetDay> dayStates = loaded.Attendance.Days
            .OrderBy(day => day.Date)
            .Select(day =>
            {
                DateOnly date = DateOnly.FromDateTime(day.Date);
                TimesheetDayEdit? update = days.GetValueOrDefault(date);
                Dictionary<Guid, decimal> projectHours = [];
                Dictionary<Guid, bool> projectHoursFixed = [];
                Dictionary<Guid, decimal> projectHoursFloor = [];

                foreach (Data.Models.ContractPart project in loaded.ContractParts)
                {
                    ContractPartColumn projectState = contractPartStatesById[project.ContractEmployeeId];
                    ContractPartEdit? contractPartUpdate = projects.GetValueOrDefault(project.ContractEmployeeId);
                    if (project.LockedAt is not null || !projectState.IsActiveOn(day.Date))
                    {
                        contractPartUpdate = null;
                    }

                    Data.Models.ContractPartDay? persistedDay = projectState.IsActiveOn(day.Date)
                        ? project.Days.FirstOrDefault(contractPartDay => DateOnly.FromDateTime(contractPartDay.Date) == date)
                        : null;
                    decimal persisted = persistedDay?.Hours ?? 0m;
                    ContractPartDayEdit? contractPartDayUpdate = contractPartUpdate?.Days.FirstOrDefault(contractPartDay => DateOnly.FromDateTime(contractPartDay.Date) == date);
                    decimal hours = contractPartDayUpdate?.Hours ?? persisted;
                    projectHours[project.ContractEmployeeId] = TimesheetLogic.Normalize(hours);
                    projectHoursFixed[project.ContractEmployeeId] = projectState.IsActiveOn(day.Date) && (contractPartDayUpdate?.HoursLocked ?? persistedDay?.HoursLocked ?? false);
                    bool projectFixed = projectHoursFixed[project.ContractEmployeeId];
                    projectHoursFloor[project.ContractEmployeeId] = projectFixed && hours > 0m ? TimesheetLogic.Normalize(hours) : 0m;
                }

                return new EditableTimesheetDay
                {
                    Date = day.Date,
                    ClockIn = update is null ? day.ClockIn : update.ClockIn,
                    ClockOut = update is null ? day.ClockOut : update.ClockOut,
                    BreakStart = update is null ? day.BreakStart : update.BreakStart,
                    BreakEnd = update is null ? day.BreakEnd : update.BreakEnd,
                    Description = update is null ? day.Description : update.Description,
                    Schedules = update is null ? ParseSchedules(day.Schedules) : update.Schedules ?? [],
                    IsHoliday = day.IsHoliday,
                    CoreHours = TimesheetLogic.Normalize(update is null ? day.CoreHours : update.CoreHours),
                    CoreHoursFixed = update?.CoreHoursFixed ?? false,
                    ContractPartHours = projectHours,
                    ContractPartHoursFixed = projectHoursFixed,
                    ContractPartHoursFloor = projectHoursFloor
                };
            })
            .ToList();

        return new EditableTimesheet(Days: dayStates, ContractParts: contractPartStates);
    }

    public static TimesheetEditRequest CurrentEditRequest(LoadedTimesheet loaded)
    {
        TimesheetDayEdit[] days = loaded.Attendance.Days.Select(day => new TimesheetDayEdit(Date: day.Date, ClockIn: day.ClockIn, ClockOut: day.ClockOut, BreakStart: day.BreakStart, BreakEnd: day.BreakEnd, CoreHours: day.CoreHours, Description: day.Description, Schedules: ParseSchedules(day.Schedules))).ToArray();
        ContractPartEdit[] projects = loaded.ContractParts.Select(project =>
        {
            ContractPartDayEdit[] contractPartDays = project.Days.Select(day => new ContractPartDayEdit(Date: day.Date, Hours: day.Hours, HoursLocked: day.HoursLocked)).ToArray();
            return new ContractPartEdit(ContractEmployeeId: project.ContractEmployeeId, Days: contractPartDays);
        }).ToArray();
        return new TimesheetEditRequest(Days: days, ContractParts: projects);
    }

    public static TimesheetEvaluation Evaluate(LoadedTimesheet loaded, TimesheetEditRequest request) => Evaluate(loaded, BuildEditableTimesheet(loaded, request));

    public static TimesheetEvaluation Evaluate(LoadedTimesheet loaded, EditableTimesheet sheet)
    {
        bool tracksAttendance = EmployeeTypes.TracksAttendance(loaded.Attendance.EmployeeTypeId);
        foreach (EditableTimesheetDay day in sheet.Days)
        {
            TimesheetInterruptionHours.ApplyToDayState(day, sheet.ContractParts, loaded.TotalWorkload, tracksAttendance);
        }

        List<AttendanceDay> attendanceDays = sheet.Days.Select(day => new AttendanceDay(Date: day.Date, ClockIn: day.ClockIn, ClockOut: day.ClockOut, BreakStart: day.BreakStart, BreakEnd: day.BreakEnd, OtherInterruption: day.Description, Schedules: day.Schedules, IsHoliday: day.IsHoliday, Workload: loaded.TotalWorkload)).ToList();
        AttendanceTimesheet attendance = new(EmployeePersonalNumber: loaded.Timesheet.Employee.PersonalNumber, EmployeeName: loaded.Timesheet.Employee.DisplayName, Workload: loaded.TotalWorkload, Year: loaded.Timesheet.Year, Month: loaded.Timesheet.Month, Days: attendanceDays);

        List<EvaluatedDay> evaluatedDays = sheet.Days.Select(day =>
        {
            decimal worked = TimesheetLogic.CalculateWorkedHoursFromAttendance(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd);
            decimal projectHours = day.ContractPartHours.Values.Sum();
            decimal stagHours = TimesheetLogic.CalculateStagHours(day.Schedules);
            bool hasAttendance = tracksAttendance && (day.ClockIn is not null || day.ClockOut is not null);
            bool skipAllocationRules = TimesheetInterruptions.SkipAllocationRules(day.Description);
            return new EvaluatedDay(Date: day.Date, IsHoliday: day.IsHoliday, Workload: loaded.TotalWorkload, CoreWorkload: loaded.CoreWorkload, WorkedHours: worked, CoreHours: day.CoreHours, ContractPartHours: projectHours, StagHours: stagHours, HasAttendanceFilled: hasAttendance, SkipAllocationRules: skipAllocationRules);
        }).ToList();

        EvaluatedTimesheet evaluated = new(Year: loaded.Timesheet.Year, Month: loaded.Timesheet.Month, CoreWorkload: loaded.CoreWorkload, Days: evaluatedDays);
        int fundedDays = sheet.Days.Count(day => TimesheetLogic.IsWorkday(day.Date, day.IsHoliday));
        List<ContractPartTotal> contractPartTotals = sheet.ContractParts.Select(project =>
        {
            decimal hours = TimesheetLogic.Normalize(sheet.Days.Sum(day => day.ContractPartHours.GetValueOrDefault(project.Id)));
            decimal obligation = TimesheetLogic.Normalize(sheet.Days.Count(day => TimesheetLogic.IsWorkday(day.Date, day.IsHoliday) && project.IsActiveOn(day.Date)) * 8m * project.Workload);
            return new ContractPartTotal(ContractEmployeeId: project.Id, Hours: hours, Obligation: obligation);
        }).ToList();

        decimal hoursObligation = TimesheetLogic.Normalize(fundedDays * 8m * loaded.TotalWorkload);
        TimesheetTotals totals = new(WorkedHours: TimesheetLogic.Normalize(evaluatedDays.Sum(day => day.WorkedHours)), HoursObligation: hoursObligation, AllocatedHours: TimesheetLogic.Normalize(evaluatedDays.Sum(day => day.AllocatedHours)), CoreHours: TimesheetLogic.Normalize(sheet.Days.Sum(day => day.CoreHours)), CoreHoursObligation: TimesheetLogic.Normalize(hoursObligation - contractPartTotals.Sum(project => project.Obligation)), ContractParts: contractPartTotals);

        TimesheetReview review = new EvaluatedTimesheetReviewer().Review(evaluated, attendance, tracksAttendance);
        IReadOnlyList<TimesheetIssue> issues = review.Issues.Concat(ReviewContractPartTotals(contractPartTotals)).Concat(ReviewCoreTolerance(totals)).ToArray();
        IReadOnlyList<DayIssue> dayIssues = review.DayIssues.ToArray();

        List<TimesheetDayEvaluation> days = sheet.Days.Zip(evaluatedDays).Select(pair =>
        {
            (EditableTimesheetDay day, EvaluatedDay evaluatedDay) = pair;
            bool businessTrip = TimesheetInterruptions.HasBusinessTripInterruption(day.Description);
            bool proportional = TimesheetInterruptions.HasProportionalInterruption(day.Description);
            decimal balance = evaluatedDay.SkipAllocationRules || !evaluatedDay.HasAttendanceFilled ? 0m : TimesheetLogic.Round(evaluatedDay.WorkedHours - evaluatedDay.AllocatedHours);
            decimal nightHours = TimesheetLogic.CalculateNightHours(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd);
            return new TimesheetDayEvaluation(Day: day.Date.Day, WorkedHours: evaluatedDay.WorkedHours, NightHours: nightHours, AllocatedHours: evaluatedDay.AllocatedHours, Balance: balance, HasBusinessTrip: businessTrip, HasCoreOnlyInterruption: false, HasProportionalInterruption: proportional);
        }).ToList();

        return new TimesheetEvaluation(HasErrors: issues.Any(issue => issue.Type is IssueType.Error) || dayIssues.Any(issue => issue.Type is IssueType.Error), Issues: issues, DayIssues: dayIssues, Days: days, Totals: totals);
    }

    private static IEnumerable<TimesheetIssue> ReviewContractPartTotals(IReadOnlyList<ContractPartTotal> contractPartTotals)
    {
        foreach (ContractPartTotal project in contractPartTotals)
        {
            if (TimesheetLogic.HasUnequalHours(project.Hours, project.Obligation))
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

    public static bool HasInactiveContractPartHours(LoadedTimesheet loaded, TimesheetEditRequest request)
    {
        foreach (ContractPartEdit project in request.ContractParts ?? [])
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

    public static void ApplyEdits(LoadedTimesheet loaded, TimesheetEditRequest request)
    {
        Dictionary<DateOnly, Data.Models.AttendanceDay> days = loaded.Attendance.Days.ToDictionary(day => DateOnly.FromDateTime(day.Date));
        foreach (TimesheetDayEdit update in request.Days)
        {
            if (!days.TryGetValue(DateOnly.FromDateTime(update.Date), out Data.Models.AttendanceDay? day))
            {
                continue;
            }

            day.ClockIn = update.ClockIn;
            day.ClockOut = update.ClockOut;
            day.BreakStart = update.BreakStart;
            day.BreakEnd = update.BreakEnd;
            day.CoreHours = TimesheetLogic.Normalize(update.CoreHours);
            day.Description = update.Description;
            day.Schedules = JsonSerializer.Serialize(update.Schedules ?? []);
            day.HoursWithoutBreak = TimesheetLogic.CalculateWorkedHoursFromAttendance(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd);
        }

        Dictionary<Guid, ContractPartEdit> projects = (request.ContractParts ?? []).ToDictionary(project => project.ContractEmployeeId);
        foreach (Data.Models.ContractPart project in loaded.ContractParts)
        {
            if (loaded.ContractPartRanges.TryGetValue(project.ContractEmployeeId, out ContractPartDateRange? range))
            {
                foreach (Data.Models.ContractPartDay day in project.Days.Where(day => !range.Includes(day.Date)))
                {
                    day.Hours = 0m;
                    day.HoursLocked = false;
                }
            }

            if (!projects.TryGetValue(project.ContractEmployeeId, out ContractPartEdit? update))
            {
                continue;
            }

            project.UpdatedAt = DateTime.UtcNow;
            if (project.LockedAt is not null)
            {
                continue;
            }

            Dictionary<DateOnly, Data.Models.ContractPartDay> contractPartDays = project.Days.ToDictionary(day => DateOnly.FromDateTime(day.Date));

            foreach (ContractPartDayEdit contractPartDay in update.Days)
            {
                if (contractPartDays.TryGetValue(DateOnly.FromDateTime(contractPartDay.Date), out Data.Models.ContractPartDay? day))
                {
                    bool active = loaded.ContractPartRanges.TryGetValue(project.ContractEmployeeId, out range) && range.Includes(contractPartDay.Date);
                    day.Hours = active ? TimesheetLogic.Normalize(contractPartDay.Hours) : 0m;
                    day.HoursLocked = active && contractPartDay.HoursLocked;
                }
            }
        }

        loaded.Timesheet.UpdatedAt = DateTime.UtcNow;
    }

    public static async Task ApplyInterruptionHoursAsync(Guid timesheetId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        LoadedTimesheet? loaded = await LoadAsync(timesheetId, dbContext, cancellationToken);
        if (loaded is null)
        {
            return;
        }

        EditableTimesheet sheet = BuildEditableTimesheet(loaded, CurrentEditRequest(loaded));
        bool tracksAttendance = EmployeeTypes.TracksAttendance(loaded.Attendance.EmployeeTypeId);
        foreach (EditableTimesheetDay day in sheet.Days)
        {
            TimesheetInterruptionHours.ApplyToDayState(day, sheet.ContractParts, loaded.TotalWorkload, tracksAttendance);
        }

        TimesheetEditRequest request = new(
            Days: sheet.Days.Select(day => new TimesheetDayEdit(day.Date, day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd, day.CoreHours, day.Description, day.Schedules)).ToList(),
            ContractParts: sheet.ContractParts.Select(project => new ContractPartEdit(
                project.Id,
                sheet.Days.Select(day => new ContractPartDayEdit(day.Date, day.ContractPartHours.GetValueOrDefault(project.Id), day.ContractPartHoursFixed.GetValueOrDefault(project.Id))).ToList())).ToList());
        ApplyEdits(loaded, request);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

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

    internal static ContractPartDateRange EffectiveContractPartRange(DateTime assignmentStartDate, DateTime? assignmentEndDate, DateTime projectStartDate, DateTime? projectEndDate)
    {
        DateTime start = Max(ToUtcDate(assignmentStartDate), ToUtcDate(projectStartDate));
        DateTime? end = Min(assignmentEndDate.HasValue ? ToUtcDate(assignmentEndDate.Value) : null, projectEndDate.HasValue ? ToUtcDate(projectEndDate.Value) : null);
        return new ContractPartDateRange(start, end);
    }

    private static List<ContractPartColumn> ContractPartColumns(LoadedTimesheet loaded) => loaded.ContractParts
        .Select(project => new ContractPartColumn(
            Id: project.ContractEmployeeId,
            Workload: project.Workload,
            Locked: project.LockedAt is not null,
            Range: loaded.ContractPartRanges.GetValueOrDefault(project.ContractEmployeeId) ?? new ContractPartDateRange(DateTime.MinValue, null)))
        .ToList();

    private static DateTime Max(DateTime first, DateTime second) => first >= second ? first : second;

    private static DateTime? Min(DateTime? first, DateTime? second) => (first, second) switch
    {
        (null, null) => null,
        (DateTime value, null) => value,
        (null, DateTime value) => value,
        (DateTime left, DateTime right) => left <= right ? left : right
    };

    private static DateTime ToUtcDate(DateTime value) => value.Kind == DateTimeKind.Utc ? value.Date : DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
}
