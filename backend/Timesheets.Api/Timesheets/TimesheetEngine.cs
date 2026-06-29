using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Employees;

namespace Timesheets.Api.Timesheets;

public sealed record TimesheetDayEdit(DateTime Date, TimeSpan? ClockIn, TimeSpan? ClockOut, TimeSpan? BreakStart, TimeSpan? BreakEnd, decimal CoreHours, string? Description, IReadOnlyList<TimeRange>? Schedules, bool CoreHoursFixed = false);

public sealed record ProjectDayEdit(DateTime Date, decimal Hours, bool HoursFixed = false);

public sealed record ProjectColumnEdit(Guid ContractEmployeeId, IReadOnlyList<ProjectDayEdit> Days);

public sealed record TimesheetEditRequest(IReadOnlyList<TimesheetDayEdit> Days, IReadOnlyList<ProjectColumnEdit>? Projects);

public sealed class TimesheetEditRequestValidator : AbstractValidator<TimesheetEditRequest>
{
    public TimesheetEditRequestValidator()
    {
        RuleFor(request => request.Days).NotEmpty().Must(HaveUniqueDates);
        RuleFor(request => request.Projects).Must(HaveUniqueProjects);
        RuleForEach(request => request.Days).ChildRules(day =>
        {
            day.RuleFor(value => value.CoreHours).InclusiveBetween(0m, 12m);
            day.RuleFor(value => value.ClockIn).Must(IsTimeOfDay);
            day.RuleFor(value => value.ClockOut).Must(IsTimeOfDay);
            day.RuleFor(value => value.BreakStart).Must(IsTimeOfDay);
            day.RuleFor(value => value.BreakEnd).Must(IsTimeOfDay);
        });
        RuleForEach(request => request.Projects).ChildRules(project =>
        {
            project.RuleFor(value => value.Days).Must(HaveUniqueDates);
            project.RuleForEach(value => value.Days).ChildRules(day =>
                day.RuleFor(value => value.Hours).InclusiveBetween(0m, 12m));
        });
    }

    private static bool IsTimeOfDay(TimeSpan? value) => value is null || value >= TimeSpan.Zero && value < TimeSpan.FromDays(1);
    private static bool HaveUniqueDates(IEnumerable<TimesheetDayEdit> days) => days.Select(day => DateOnly.FromDateTime(day.Date)).Distinct().Count() == days.Count();
    private static bool HaveUniqueDates(IEnumerable<ProjectDayEdit> days) => days.Select(day => DateOnly.FromDateTime(day.Date)).Distinct().Count() == days.Count();
    private static bool HaveUniqueProjects(IEnumerable<ProjectColumnEdit>? projects) => projects is null || projects.Select(project => project.ContractEmployeeId).Distinct().Count() == projects.Count();
}

public sealed record TimesheetDayEvaluation(int Day, decimal WorkedHours, decimal NightHours, decimal AllocatedHours, decimal Balance, bool HasBusinessTrip, bool HasCoreOnlyInterruption, bool HasProportionalInterruption);

public sealed record TimesheetProjectTotal(Guid ProjectId, decimal Hours, decimal Obligation);

public sealed record TimesheetTotals(decimal WorkedHours, decimal HoursObligation, decimal AllocatedHours, decimal CoreHours, decimal CoreHoursObligation, IReadOnlyList<TimesheetProjectTotal> Projects);

public sealed record TimesheetEvaluation(bool HasErrors, IReadOnlyList<TimesheetIssue> Issues, IReadOnlyList<DayIssue> DayIssues, IReadOnlyList<TimesheetDayEvaluation> Days, TimesheetTotals Totals);

public sealed record ProjectDateRange(DateTime StartDate, DateTime? EndDate)
{
    public bool Includes(DateTime date)
    {
        DateTime value = ToUtcDate(date);
        return value >= ToUtcDate(StartDate) && (!EndDate.HasValue || value <= ToUtcDate(EndDate.Value));
    }

    private static DateTime ToUtcDate(DateTime value) => value.Kind == DateTimeKind.Utc ? value.Date : DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
}

public sealed record LoadedTimesheet(Data.Models.AttendanceTimesheet Timesheet, Guid? EmployeeTypeId, IReadOnlyList<Data.Models.ProjectTimesheet> Projects, IReadOnlyDictionary<Guid, ProjectDateRange> ProjectRanges, decimal TotalWorkload, decimal CoreWorkload);

public sealed record ProjectColumn(Guid Id, decimal Workload, bool Locked, ProjectDateRange Range)
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
    public required Dictionary<Guid, decimal> ProjectHours { get; init; }
    public required Dictionary<Guid, bool> ProjectHoursFixed { get; init; }
}

public sealed record EditableTimesheet(IReadOnlyList<EditableTimesheetDay> Days, IReadOnlyList<ProjectColumn> Projects);

public static class TimesheetEngine
{
    public static async Task<LoadedTimesheet?> LoadAsync(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Data.Models.AttendanceTimesheet? timesheet = await dbContext.AttendanceTimesheets
            .Include(value => value.Employee)
            .Include(value => value.TimesheetStatus)
            .Include(value => value.Days)
            .SingleOrDefaultAsync(value => value.Id == id, cancellationToken);

        if (timesheet is null)
        {
            return null;
        }

        List<Data.Models.ProjectTimesheet> projects = await dbContext.ProjectTimesheets
            .Include(value => value.Days)
            .Where(value => value.EmployeeId == timesheet.EmployeeId && value.Year == timesheet.Year && value.Month == timesheet.Month)
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
        Dictionary<Guid, ProjectDateRange> projectRanges = rangeRows.ToDictionary(
            row => row.Id,
            row => EffectiveProjectRange(row.StartDate, row.AssignmentEndDate, row.ProjectStartDate, row.ProjectEndDate));

        decimal totalWorkload = await TimesheetWorkloads.GetAsync(timesheet.EmployeeId, timesheet.Year, timesheet.Month, dbContext, cancellationToken);
        decimal coreWorkload = Math.Max(0m, totalWorkload - projects.Sum(project => project.Workload));
        Guid? employeeTypeId = timesheet.EmployeeTypeId;
        return new LoadedTimesheet(Timesheet: timesheet, EmployeeTypeId: employeeTypeId, Projects: projects, ProjectRanges: projectRanges, TotalWorkload: totalWorkload, CoreWorkload: coreWorkload);
    }

    public static EditableTimesheet BuildEditableTimesheet(LoadedTimesheet loaded, TimesheetEditRequest request)
    {
        Dictionary<DateOnly, TimesheetDayEdit> days = request.Days.ToDictionary(day => DateOnly.FromDateTime(day.Date));
        Dictionary<Guid, ProjectColumnEdit> projects = (request.Projects ?? []).ToDictionary(project => project.ContractEmployeeId);
        List<ProjectColumn> projectStates = ProjectColumns(loaded);
        Dictionary<Guid, ProjectColumn> projectStatesById = projectStates.ToDictionary(project => project.Id);

        List<EditableTimesheetDay> dayStates = loaded.Timesheet.Days
            .OrderBy(day => day.Date)
            .Select(day =>
            {
                DateOnly date = DateOnly.FromDateTime(day.Date);
                TimesheetDayEdit? update = days.GetValueOrDefault(date);
                Dictionary<Guid, decimal> projectHours = [];
                Dictionary<Guid, bool> projectHoursFixed = [];

                foreach (Data.Models.ProjectTimesheet project in loaded.Projects)
                {
                    ProjectColumn projectState = projectStatesById[project.ContractEmployeeId];
                    ProjectColumnEdit? projectUpdate = projects.GetValueOrDefault(project.ContractEmployeeId);
                    if (project.LockedAt is not null || !projectState.IsActiveOn(day.Date))
                    {
                        projectUpdate = null;
                    }

                    decimal persisted = projectState.IsActiveOn(day.Date)
                        ? project.Days.FirstOrDefault(projectDay => DateOnly.FromDateTime(projectDay.Date) == date)?.Hours ?? 0m
                        : 0m;
                    ProjectDayEdit? projectDayUpdate = projectUpdate?.Days.FirstOrDefault(projectDay => DateOnly.FromDateTime(projectDay.Date) == date);
                    decimal hours = projectDayUpdate?.Hours ?? persisted;
                    projectHours[project.ContractEmployeeId] = TimesheetLogic.Normalize(hours);
                    projectHoursFixed[project.ContractEmployeeId] = projectState.IsActiveOn(day.Date) && (projectDayUpdate?.HoursFixed ?? false);
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
                    ProjectHours = projectHours,
                    ProjectHoursFixed = projectHoursFixed
                };
            })
            .ToList();

        return new EditableTimesheet(Days: dayStates, Projects: projectStates);
    }

    public static TimesheetEditRequest CurrentEditRequest(LoadedTimesheet loaded)
    {
        TimesheetDayEdit[] days = loaded.Timesheet.Days.Select(day => new TimesheetDayEdit(Date: day.Date, ClockIn: day.ClockIn, ClockOut: day.ClockOut, BreakStart: day.BreakStart, BreakEnd: day.BreakEnd, CoreHours: day.CoreHours, Description: day.Description, Schedules: ParseSchedules(day.Schedules))).ToArray();
        ProjectColumnEdit[] projects = loaded.Projects.Select(project =>
        {
            ProjectDayEdit[] projectDays = project.Days.Select(day => new ProjectDayEdit(Date: day.Date, Hours: day.Hours)).ToArray();
            return new ProjectColumnEdit(ContractEmployeeId: project.ContractEmployeeId, Days: projectDays);
        }).ToArray();
        return new TimesheetEditRequest(Days: days, Projects: projects);
    }

    public static TimesheetEvaluation Evaluate(LoadedTimesheet loaded, TimesheetEditRequest request) => Evaluate(loaded, BuildEditableTimesheet(loaded, request));

    public static TimesheetEvaluation Evaluate(LoadedTimesheet loaded, EditableTimesheet sheet)
    {
        bool tracksAttendance = EmployeeTypes.TracksAttendance(loaded.EmployeeTypeId);
        foreach (EditableTimesheetDay day in sheet.Days)
        {
            TimesheetInterruptionHours.ApplyToDayState(day, sheet.Projects, loaded.TotalWorkload, tracksAttendance);
        }

        List<AttendanceDay> attendanceDays = sheet.Days.Select(day => new AttendanceDay(Date: day.Date, ClockIn: day.ClockIn, ClockOut: day.ClockOut, BreakStart: day.BreakStart, BreakEnd: day.BreakEnd, OtherInterruption: day.Description, Schedules: day.Schedules, IsHoliday: day.IsHoliday, Workload: loaded.TotalWorkload)).ToList();
        AttendanceTimesheet attendance = new(EmployeePersonalNumber: loaded.Timesheet.Employee.PersonalNumber, EmployeeName: loaded.Timesheet.Employee.FullName, Workload: loaded.TotalWorkload, Year: loaded.Timesheet.Year, Month: loaded.Timesheet.Month, Days: attendanceDays);

        List<CombinedDay> combinedDays = sheet.Days.Select(day =>
        {
            decimal worked = TimesheetLogic.CalculateWorkedHoursFromAttendance(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd);
            decimal projectHours = day.ProjectHours.Values.Sum();
            decimal stagHours = TimesheetLogic.CalculateStagHours(day.Schedules);
            bool hasAttendance = tracksAttendance && (day.ClockIn is not null || day.ClockOut is not null);
            bool skipAllocationRules = TimesheetInterruptions.SkipAllocationRules(day.Description);
            return new CombinedDay(Date: day.Date, IsHoliday: day.IsHoliday, Workload: loaded.TotalWorkload, CoreWorkload: loaded.CoreWorkload, WorkedHours: worked, CoreHours: day.CoreHours, ProjectHours: projectHours, StagHours: stagHours, HasAttendanceFilled: hasAttendance, SkipAllocationRules: skipAllocationRules);
        }).ToList();

        CombinedTimesheet combined = new(Year: loaded.Timesheet.Year, Month: loaded.Timesheet.Month, CoreWorkload: loaded.CoreWorkload, Days: combinedDays);
        TimesheetReview review = new CombinedTimesheetReviewer().Review(combined, attendance, tracksAttendance);
        IReadOnlyList<TimesheetIssue> issues = review.Issues.ToArray();
        IReadOnlyList<DayIssue> dayIssues = review.DayIssues.ToArray();

        List<TimesheetDayEvaluation> days = sheet.Days.Zip(combinedDays).Select(pair =>
        {
            (EditableTimesheetDay day, CombinedDay combinedDay) = pair;
            bool businessTrip = TimesheetInterruptions.HasBusinessTripInterruption(day.Description);
            bool proportional = TimesheetInterruptions.HasProportionalInterruption(day.Description);
            decimal balance = combinedDay.SkipAllocationRules || !combinedDay.HasAttendanceFilled ? 0m : TimesheetLogic.Round(combinedDay.WorkedHours - combinedDay.AllocatedHours);
            decimal nightHours = TimesheetLogic.CalculateNightHours(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd);
            return new TimesheetDayEvaluation(Day: day.Date.Day, WorkedHours: combinedDay.WorkedHours, NightHours: nightHours, AllocatedHours: combinedDay.AllocatedHours, Balance: balance, HasBusinessTrip: businessTrip, HasCoreOnlyInterruption: false, HasProportionalInterruption: proportional);
        }).ToList();

        int fundedDays = sheet.Days.Count(day => TimesheetLogic.IsWorkday(day.Date, day.IsHoliday));
        List<TimesheetProjectTotal> projectTotals = sheet.Projects.Select(project =>
        {
            decimal hours = TimesheetLogic.Normalize(sheet.Days.Sum(day => day.ProjectHours.GetValueOrDefault(project.Id)));
            decimal obligation = TimesheetLogic.Normalize(sheet.Days.Count(day => TimesheetLogic.IsWorkday(day.Date, day.IsHoliday) && project.IsActiveOn(day.Date)) * 8m * project.Workload);
            return new TimesheetProjectTotal(ProjectId: project.Id, Hours: hours, Obligation: obligation);
        }).ToList();

        decimal hoursObligation = TimesheetLogic.Normalize(fundedDays * 8m * loaded.TotalWorkload);
        TimesheetTotals totals = new(WorkedHours: TimesheetLogic.Normalize(combinedDays.Sum(day => day.WorkedHours)), HoursObligation: hoursObligation, AllocatedHours: TimesheetLogic.Normalize(combinedDays.Sum(day => day.AllocatedHours)), CoreHours: TimesheetLogic.Normalize(sheet.Days.Sum(day => day.CoreHours)), CoreHoursObligation: TimesheetLogic.Normalize(hoursObligation - projectTotals.Sum(project => project.Obligation)), Projects: projectTotals);

        return new TimesheetEvaluation(HasErrors: review.HasErrors, Issues: issues, DayIssues: dayIssues, Days: days, Totals: totals);
    }

    public static bool HasInactiveProjectHours(LoadedTimesheet loaded, TimesheetEditRequest request)
    {
        foreach (ProjectColumnEdit project in request.Projects ?? [])
        {
            if (!loaded.ProjectRanges.TryGetValue(project.ContractEmployeeId, out ProjectDateRange? range))
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
        Dictionary<DateOnly, Data.Models.AttendanceDay> days = loaded.Timesheet.Days.ToDictionary(day => DateOnly.FromDateTime(day.Date));
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

        Dictionary<Guid, ProjectColumnEdit> projects = (request.Projects ?? []).ToDictionary(project => project.ContractEmployeeId);
        foreach (Data.Models.ProjectTimesheet project in loaded.Projects)
        {
            if (loaded.ProjectRanges.TryGetValue(project.ContractEmployeeId, out ProjectDateRange? range))
            {
                foreach (Data.Models.ProjectDay day in project.Days.Where(day => !range.Includes(day.Date)))
                {
                    day.Hours = 0m;
                }
            }

            if (!projects.TryGetValue(project.ContractEmployeeId, out ProjectColumnEdit? update))
            {
                continue;
            }

            project.UpdatedAt = DateTime.UtcNow;
            if (project.LockedAt is not null)
            {
                continue;
            }

            Dictionary<DateOnly, Data.Models.ProjectDay> projectDays = project.Days.ToDictionary(day => DateOnly.FromDateTime(day.Date));

            foreach (ProjectDayEdit projectDay in update.Days)
            {
                if (projectDays.TryGetValue(DateOnly.FromDateTime(projectDay.Date), out Data.Models.ProjectDay? day))
                {
                    bool active = loaded.ProjectRanges.TryGetValue(project.ContractEmployeeId, out range) && range.Includes(projectDay.Date);
                    day.Hours = active ? TimesheetLogic.Normalize(projectDay.Hours) : 0m;
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
        bool tracksAttendance = EmployeeTypes.TracksAttendance(loaded.EmployeeTypeId);
        foreach (EditableTimesheetDay day in sheet.Days)
        {
            TimesheetInterruptionHours.ApplyToDayState(day, sheet.Projects, loaded.TotalWorkload, tracksAttendance);
        }

        TimesheetEditRequest request = new(
            Days: sheet.Days.Select(day => new TimesheetDayEdit(day.Date, day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd, day.CoreHours, day.Description, day.Schedules)).ToList(),
            Projects: sheet.Projects.Select(project => new ProjectColumnEdit(
                project.Id,
                sheet.Days.Select(day => new ProjectDayEdit(day.Date, day.ProjectHours.GetValueOrDefault(project.Id))).ToList())).ToList());
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

    internal static ProjectDateRange EffectiveProjectRange(DateTime assignmentStartDate, DateTime? assignmentEndDate, DateTime projectStartDate, DateTime? projectEndDate)
    {
        DateTime start = Max(ToUtcDate(assignmentStartDate), ToUtcDate(projectStartDate));
        DateTime? end = Min(assignmentEndDate.HasValue ? ToUtcDate(assignmentEndDate.Value) : null, projectEndDate.HasValue ? ToUtcDate(projectEndDate.Value) : null);
        return new ProjectDateRange(start, end);
    }

    private static List<ProjectColumn> ProjectColumns(LoadedTimesheet loaded) => loaded.Projects
        .Select(project => new ProjectColumn(
            Id: project.ContractEmployeeId,
            Workload: project.Workload,
            Locked: project.LockedAt is not null,
            Range: loaded.ProjectRanges.GetValueOrDefault(project.ContractEmployeeId) ?? new ProjectDateRange(DateTime.MinValue, null)))
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
