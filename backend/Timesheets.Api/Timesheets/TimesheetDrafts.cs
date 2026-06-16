using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Employees;

namespace Timesheets.Api.Timesheets;

public sealed record TimesheetDraftDay(DateTime Date, TimeSpan? ClockIn, TimeSpan? ClockOut, TimeSpan? BreakStart, TimeSpan? BreakEnd, decimal CoreHours, string? Description, IReadOnlyList<TimeRange>? Schedules, bool CoreHoursFixed = false);

public sealed record TimesheetDraftProjectDay(DateTime Date, decimal Hours, bool HoursFixed = false);

public sealed record TimesheetDraftProject(Guid ContractEmployeeId, IReadOnlyList<TimesheetDraftProjectDay> Days);

public sealed record TimesheetDraft(IReadOnlyList<TimesheetDraftDay> Days, IReadOnlyList<TimesheetDraftProject>? Projects);

public sealed class TimesheetDraftValidator : AbstractValidator<TimesheetDraft>
{
    public TimesheetDraftValidator()
    {
        RuleFor(draft => draft.Days).NotEmpty().Must(HaveUniqueDates);
        RuleFor(draft => draft.Projects).Must(HaveUniqueProjects);
        RuleForEach(draft => draft.Days).ChildRules(day =>
        {
            day.RuleFor(value => value.CoreHours).InclusiveBetween(0m, 12m);
            day.RuleFor(value => value.ClockIn).Must(IsTimeOfDay);
            day.RuleFor(value => value.ClockOut).Must(IsTimeOfDay);
            day.RuleFor(value => value.BreakStart).Must(IsTimeOfDay);
            day.RuleFor(value => value.BreakEnd).Must(IsTimeOfDay);
        });
        RuleForEach(draft => draft.Projects).ChildRules(project =>
        {
            project.RuleFor(value => value.Days).Must(HaveUniqueDates);
            project.RuleForEach(value => value.Days).ChildRules(day =>
                day.RuleFor(value => value.Hours).InclusiveBetween(0m, 12m));
        });
    }

    private static bool IsTimeOfDay(TimeSpan? value) => value is null || value >= TimeSpan.Zero && value < TimeSpan.FromDays(1);
    private static bool HaveUniqueDates(IEnumerable<TimesheetDraftDay> days) => days.Select(day => DateOnly.FromDateTime(day.Date)).Distinct().Count() == days.Count();
    private static bool HaveUniqueDates(IEnumerable<TimesheetDraftProjectDay> days) => days.Select(day => DateOnly.FromDateTime(day.Date)).Distinct().Count() == days.Count();
    private static bool HaveUniqueProjects(IEnumerable<TimesheetDraftProject>? projects) => projects is null || projects.Select(project => project.ContractEmployeeId).Distinct().Count() == projects.Count();
}

public sealed record TimesheetDayEvaluation(int Day, decimal WorkedHours, decimal NightHours, decimal AllocatedHours, decimal Balance, bool HasBusinessTrip, bool HasCoreOnlyInterruption, bool HasProportionalInterruption);

public sealed record TimesheetProjectTotal(Guid ProjectId, decimal Hours, decimal Obligation);

public sealed record TimesheetTotals(decimal WorkedHours, decimal HoursObligation, decimal AllocatedHours, decimal CoreHours, decimal CoreHoursObligation, IReadOnlyList<TimesheetProjectTotal> Projects);

public sealed record TimesheetEvaluation(bool HasErrors, IReadOnlyList<TimesheetIssue> Issues, IReadOnlyList<DayIssue> DayIssues, IReadOnlyList<TimesheetDayEvaluation> Days, TimesheetTotals Totals);

public sealed record TimesheetAllocationDay(DateTime Date, int?[] Work, int?[] Break, decimal CoreHours, IReadOnlyDictionary<Guid, decimal> ProjectHours);
public sealed record TimesheetAllocation(IReadOnlyList<TimesheetAllocationDay> Days, TimesheetEvaluation Evaluation);

internal sealed record TimesheetDraftContext(Data.Models.AttendanceTimesheet Timesheet, IReadOnlyList<Data.Models.ProjectTimesheet> Projects, decimal TotalWorkload, decimal CoreWorkload);

internal sealed record TimesheetDraftProjectState(Guid Id, decimal Workload, bool Locked);

internal sealed class TimesheetDraftDayState
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

internal sealed record TimesheetDraftSnapshot(IReadOnlyList<TimesheetDraftDayState> Days, IReadOnlyList<TimesheetDraftProjectState> Projects);

internal static class TimesheetDrafts
{
    public static async Task<TimesheetDraftContext?> LoadAsync(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
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

        decimal totalWorkload = await TimesheetWorkloads.GetAsync(timesheet.EmployeeId, timesheet.Year, timesheet.Month, dbContext, cancellationToken);
        decimal coreWorkload = Math.Max(0m, totalWorkload - projects.Sum(project => project.Workload));
        return new TimesheetDraftContext(Timesheet: timesheet, Projects: projects, TotalWorkload: totalWorkload, CoreWorkload: coreWorkload);
    }

    public static TimesheetDraftSnapshot BuildSnapshot(TimesheetDraftContext context, TimesheetDraft draft)
    {
        Dictionary<DateOnly, TimesheetDraftDay> days = draft.Days.ToDictionary(day => DateOnly.FromDateTime(day.Date));
        Dictionary<Guid, TimesheetDraftProject> projects = (draft.Projects ?? []).ToDictionary(project => project.ContractEmployeeId);
        List<TimesheetDraftProjectState> projectStates = context.Projects
            .Select(project =>
            {
                TimesheetDraftProject? update = projects.GetValueOrDefault(project.ContractEmployeeId);
                return new TimesheetDraftProjectState(Id: project.ContractEmployeeId, Workload: project.Workload, Locked: project.LockedAt is not null);
            })
            .ToList();

        List<TimesheetDraftDayState> dayStates = context.Timesheet.Days
            .OrderBy(day => day.Date)
            .Select(day =>
            {
                DateOnly date = DateOnly.FromDateTime(day.Date);
                TimesheetDraftDay? update = days.GetValueOrDefault(date);
                Dictionary<Guid, decimal> projectHours = [];
                Dictionary<Guid, bool> projectHoursFixed = [];

                foreach (Data.Models.ProjectTimesheet project in context.Projects)
                {
                    TimesheetDraftProject? projectUpdate = projects.GetValueOrDefault(project.ContractEmployeeId);
                    if (project.LockedAt is not null)
                    {
                        projectUpdate = null;
                    }

                    decimal persisted = project.Days.FirstOrDefault(projectDay => DateOnly.FromDateTime(projectDay.Date) == date)?.Hours ?? 0m;
                    TimesheetDraftProjectDay? projectDayUpdate = projectUpdate?.Days.FirstOrDefault(projectDay => DateOnly.FromDateTime(projectDay.Date) == date);
                    decimal hours = projectDayUpdate?.Hours ?? persisted;
                    projectHours[project.ContractEmployeeId] = TimesheetLogic.Normalize(hours);
                    projectHoursFixed[project.ContractEmployeeId] = projectDayUpdate?.HoursFixed ?? false;
                }

                return new TimesheetDraftDayState
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

        return new TimesheetDraftSnapshot(Days: dayStates, Projects: projectStates);
    }

    public static TimesheetDraft Current(TimesheetDraftContext context)
    {
        TimesheetDraftDay[] days = context.Timesheet.Days.Select(day => new TimesheetDraftDay(Date: day.Date, ClockIn: day.ClockIn, ClockOut: day.ClockOut, BreakStart: day.BreakStart, BreakEnd: day.BreakEnd, CoreHours: day.CoreHours, Description: day.Description, Schedules: ParseSchedules(day.Schedules))).ToArray();
        TimesheetDraftProject[] projects = context.Projects.Select(project =>
        {
            TimesheetDraftProjectDay[] projectDays = project.Days.Select(day => new TimesheetDraftProjectDay(Date: day.Date, Hours: day.Hours)).ToArray();
            return new TimesheetDraftProject(ContractEmployeeId: project.ContractEmployeeId, Days: projectDays);
        }).ToArray();
        return new TimesheetDraft(Days: days, Projects: projects);
    }

    public static TimesheetEvaluation Evaluate(TimesheetDraftContext context, TimesheetDraft draft) => Evaluate(context, BuildSnapshot(context, draft));

    public static TimesheetEvaluation Evaluate(TimesheetDraftContext context, TimesheetDraftSnapshot snapshot)
    {
        bool tracksAttendance = EmployeeTypes.TracksAttendance(context.Timesheet.Employee.EmployeeTypeId);
        foreach (TimesheetDraftDayState day in snapshot.Days)
        {
            TimesheetInterruptionHours.ApplyToDayState(day, snapshot.Projects, context.TotalWorkload, tracksAttendance);
        }

        List<AttendanceDay> attendanceDays = snapshot.Days.Select(day => new AttendanceDay(Date: day.Date, ClockIn: day.ClockIn, ClockOut: day.ClockOut, BreakStart: day.BreakStart, BreakEnd: day.BreakEnd, OtherInterruption: day.Description, Schedules: day.Schedules, IsHoliday: day.IsHoliday, Workload: context.TotalWorkload)).ToList();
        AttendanceTimesheet attendance = new(EmployeePersonalNumber: context.Timesheet.Employee.PersonalNumber, EmployeeName: context.Timesheet.Employee.FullName, Workload: context.TotalWorkload, Year: context.Timesheet.Year, Month: context.Timesheet.Month, Days: attendanceDays);

        List<CombinedDay> combinedDays = snapshot.Days.Select(day =>
        {
            decimal worked = TimesheetLogic.CalculateWorkedHoursFromAttendance(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd);
            decimal projectHours = day.ProjectHours.Values.Sum();
            decimal stagHours = TimesheetLogic.CalculateStagHours(day.Schedules);
            bool hasAttendance = tracksAttendance && (day.ClockIn is not null || day.ClockOut is not null);
            bool skipAllocationRules = TimesheetInterruptions.SkipAllocationRules(day.Description);
            return new CombinedDay(Date: day.Date, IsHoliday: day.IsHoliday, Workload: context.TotalWorkload, CoreWorkload: context.CoreWorkload, WorkedHours: worked, CoreHours: day.CoreHours, ProjectHours: projectHours, StagHours: stagHours, HasAttendanceFilled: hasAttendance, SkipAllocationRules: skipAllocationRules);
        }).ToList();

        CombinedTimesheet combined = new(Year: context.Timesheet.Year, Month: context.Timesheet.Month, CoreWorkload: context.CoreWorkload, Days: combinedDays);
        TimesheetReview review = new CombinedTimesheetReviewer().Review(combined, attendance, tracksAttendance);
        IReadOnlyList<TimesheetIssue> issues = review.Issues.ToArray();
        IReadOnlyList<DayIssue> dayIssues = review.DayIssues.ToArray();

        List<TimesheetDayEvaluation> days = snapshot.Days.Zip(combinedDays).Select(pair =>
        {
            (TimesheetDraftDayState day, CombinedDay combinedDay) = pair;
            bool businessTrip = TimesheetInterruptions.HasBusinessTripInterruption(day.Description);
            bool proportional = TimesheetInterruptions.HasProportionalInterruption(day.Description);
            decimal balance = combinedDay.SkipAllocationRules || !combinedDay.HasAttendanceFilled ? 0m : TimesheetLogic.Round(combinedDay.WorkedHours - combinedDay.AllocatedHours);
            decimal nightHours = TimesheetLogic.CalculateNightHours(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd);
            return new TimesheetDayEvaluation(Day: day.Date.Day, WorkedHours: combinedDay.WorkedHours, NightHours: nightHours, AllocatedHours: combinedDay.AllocatedHours, Balance: balance, HasBusinessTrip: businessTrip, HasCoreOnlyInterruption: false, HasProportionalInterruption: proportional);
        }).ToList();

        int fundedDays = snapshot.Days.Count(day => TimesheetLogic.IsWeekday(day.Date));
        List<TimesheetProjectTotal> projectTotals = snapshot.Projects.Select(project =>
        {
            decimal hours = TimesheetLogic.Normalize(snapshot.Days.Sum(day => day.ProjectHours.GetValueOrDefault(project.Id)));
            decimal obligation = TimesheetLogic.Normalize(fundedDays * 8m * project.Workload);
            return new TimesheetProjectTotal(ProjectId: project.Id, Hours: hours, Obligation: obligation);
        }).ToList();

        TimesheetTotals totals = new(WorkedHours: TimesheetLogic.Normalize(combinedDays.Sum(day => day.WorkedHours)), HoursObligation: TimesheetLogic.Normalize(fundedDays * 8m * context.TotalWorkload), AllocatedHours: TimesheetLogic.Normalize(combinedDays.Sum(day => day.AllocatedHours)), CoreHours: TimesheetLogic.Normalize(snapshot.Days.Sum(day => day.CoreHours)), CoreHoursObligation: TimesheetLogic.Normalize(fundedDays * 8m * context.CoreWorkload), Projects: projectTotals);

        return new TimesheetEvaluation(HasErrors: review.HasErrors, Issues: issues, DayIssues: dayIssues, Days: days, Totals: totals);
    }

    public static void Apply(TimesheetDraftContext context, TimesheetDraft draft)
    {
        Dictionary<DateOnly, Data.Models.AttendanceDay> days = context.Timesheet.Days.ToDictionary(day => DateOnly.FromDateTime(day.Date));
        foreach (TimesheetDraftDay update in draft.Days)
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

        Dictionary<Guid, TimesheetDraftProject> projects = (draft.Projects ?? []).ToDictionary(project => project.ContractEmployeeId);
        foreach (Data.Models.ProjectTimesheet project in context.Projects)
        {
            if (!projects.TryGetValue(project.ContractEmployeeId, out TimesheetDraftProject? update))
            {
                continue;
            }

            project.UpdatedAt = DateTime.UtcNow;
            if (project.LockedAt is not null)
            {
                continue;
            }

            Dictionary<DateOnly, Data.Models.ProjectDay> projectDays = project.Days.ToDictionary(day => DateOnly.FromDateTime(day.Date));

            foreach (TimesheetDraftProjectDay projectDay in update.Days)
            {
                if (projectDays.TryGetValue(DateOnly.FromDateTime(projectDay.Date), out Data.Models.ProjectDay? day))
                {
                    day.Hours = TimesheetLogic.Normalize(projectDay.Hours);
                }
            }
        }

        context.Timesheet.UpdatedAt = DateTime.UtcNow;
    }

    public static async Task ApplyInterruptionHoursAsync(Guid timesheetId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        TimesheetDraftContext? context = await LoadAsync(timesheetId, dbContext, cancellationToken);
        if (context is null)
        {
            return;
        }

        TimesheetDraftSnapshot snapshot = BuildSnapshot(context, Current(context));
        bool tracksAttendance = EmployeeTypes.TracksAttendance(context.Timesheet.Employee.EmployeeTypeId);
        foreach (TimesheetDraftDayState day in snapshot.Days)
        {
            TimesheetInterruptionHours.ApplyToDayState(day, snapshot.Projects, context.TotalWorkload, tracksAttendance);
        }

        TimesheetDraft draft = new(
            Days: snapshot.Days.Select(day => new TimesheetDraftDay(day.Date, day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd, day.CoreHours, day.Description, day.Schedules)).ToList(),
            Projects: snapshot.Projects.Select(project => new TimesheetDraftProject(
                project.Id,
                snapshot.Days.Select(day => new TimesheetDraftProjectDay(day.Date, day.ProjectHours.GetValueOrDefault(project.Id))).ToList())).ToList());
        Apply(context, draft);
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
}
