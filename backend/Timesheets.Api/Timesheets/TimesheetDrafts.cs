using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Timesheets;

public sealed record TimesheetDraftDay(
    DateTime Date,
    TimeSpan? ClockIn,
    TimeSpan? ClockOut,
    TimeSpan? BreakStart,
    TimeSpan? BreakEnd,
    decimal CoreHours,
    string? Description,
    IReadOnlyList<TimeRange>? Schedules);

public sealed record TimesheetDraftProjectDay(DateTime Date, decimal Hours);

public sealed record TimesheetDraftProject(
    Guid ContractEmployeeId,
    DateTime? LockedAt,
    Guid? LockedBy,
    IReadOnlyList<TimesheetDraftProjectDay> Days);

public sealed record TimesheetDraft(
    IReadOnlyList<TimesheetDraftDay> Days,
    IReadOnlyList<TimesheetDraftProject>? Projects);

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

public sealed record TimesheetDayEvaluation(
    int Day,
    decimal WorkedHours,
    decimal NightHours,
    decimal AllocatedHours,
    decimal Balance,
    bool HasBusinessTrip,
    bool HasCoreOnlyInterruption,
    bool HasProportionalInterruption);

public sealed record TimesheetProjectTotal(Guid ProjectId, decimal Hours, decimal Obligation);

public sealed record TimesheetTotals(
    decimal WorkedHours,
    decimal HoursObligation,
    decimal AllocatedHours,
    decimal CoreHours,
    decimal CoreHoursObligation,
    IReadOnlyList<TimesheetProjectTotal> Projects);

public sealed record TimesheetEvaluation(
    bool HasErrors,
    IReadOnlyList<TimesheetIssue> Issues,
    IReadOnlyList<DayIssue> DayIssues,
    IReadOnlyList<TimesheetDayEvaluation> Days,
    TimesheetTotals Totals);

public sealed record TimesheetAllocationDay(DateTime Date, decimal CoreHours, IReadOnlyDictionary<Guid, decimal> ProjectHours);
public sealed record TimesheetAllocation(IReadOnlyList<TimesheetAllocationDay> Days, TimesheetEvaluation Evaluation);

internal sealed record TimesheetDraftContext(
    Data.Models.AttendanceTimesheet Timesheet,
    IReadOnlyList<Data.Models.ProjectTimesheet> Projects,
    decimal TotalWorkload,
    decimal CoreWorkload);

internal sealed record TimesheetDraftProjectState(Guid Id, decimal Workload, DateTime? LockedAt, Guid? LockedBy);

internal sealed class TimesheetDraftDayState
{
    public required DateTime Date { get; init; }
    public required TimeSpan? ClockIn { get; init; }
    public required TimeSpan? ClockOut { get; init; }
    public required TimeSpan? BreakStart { get; init; }
    public required TimeSpan? BreakEnd { get; init; }
    public required string? Description { get; init; }
    public required IReadOnlyList<TimeRange> Schedules { get; init; }
    public required bool IsHoliday { get; init; }
    public required decimal CoreHours { get; set; }
    public required Dictionary<Guid, decimal> ProjectHours { get; init; }
}

internal sealed record TimesheetDraftSnapshot(
    IReadOnlyList<TimesheetDraftDayState> Days,
    IReadOnlyList<TimesheetDraftProjectState> Projects);

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
        return new TimesheetDraftContext(timesheet, projects, totalWorkload, coreWorkload);
    }

    public static TimesheetDraftSnapshot BuildSnapshot(TimesheetDraftContext context, TimesheetDraft draft)
    {
        Dictionary<DateOnly, TimesheetDraftDay> days = draft.Days.ToDictionary(day => DateOnly.FromDateTime(day.Date));
        Dictionary<Guid, TimesheetDraftProject> projects = (draft.Projects ?? []).ToDictionary(project => project.ContractEmployeeId);
        List<TimesheetDraftProjectState> projectStates = context.Projects
            .Select(project =>
            {
                TimesheetDraftProject? update = projects.GetValueOrDefault(project.ContractEmployeeId);
                return new TimesheetDraftProjectState(
                    project.ContractEmployeeId,
                    project.Workload,
                    update is null ? project.LockedAt : update.LockedAt,
                    update is null ? project.LockedBy : update.LockedBy);
            })
            .ToList();

        List<TimesheetDraftDayState> dayStates = context.Timesheet.Days
            .OrderBy(day => day.Date)
            .Select(day =>
            {
                DateOnly date = DateOnly.FromDateTime(day.Date);
                TimesheetDraftDay? update = days.GetValueOrDefault(date);
                Dictionary<Guid, decimal> projectHours = [];

                foreach (Data.Models.ProjectTimesheet project in context.Projects)
                {
                    TimesheetDraftProject? projectUpdate = projects.GetValueOrDefault(project.ContractEmployeeId);
                    decimal persisted = project.Days.FirstOrDefault(projectDay => DateOnly.FromDateTime(projectDay.Date) == date)?.Hours ?? 0m;
                    decimal hours = projectUpdate?.Days.FirstOrDefault(projectDay => DateOnly.FromDateTime(projectDay.Date) == date)?.Hours ?? persisted;
                    projectHours[project.ContractEmployeeId] = TimesheetLogic.Normalize(hours);
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
                    ProjectHours = projectHours
                };
            })
            .ToList();

        return new TimesheetDraftSnapshot(dayStates, projectStates);
    }

    public static TimesheetDraft Current(TimesheetDraftContext context) => new(
        context.Timesheet.Days.Select(day => new TimesheetDraftDay(
            day.Date,
            day.ClockIn,
            day.ClockOut,
            day.BreakStart,
            day.BreakEnd,
            day.CoreHours,
            day.Description,
            ParseSchedules(day.Schedules))).ToArray(),
        context.Projects.Select(project => new TimesheetDraftProject(
            project.ContractEmployeeId,
            project.LockedAt,
            project.LockedBy,
            project.Days.Select(day => new TimesheetDraftProjectDay(day.Date, day.Hours)).ToArray())).ToArray());

    public static TimesheetEvaluation Evaluate(TimesheetDraftContext context, TimesheetDraft draft) =>
        Evaluate(context, BuildSnapshot(context, draft));

    public static TimesheetEvaluation Evaluate(TimesheetDraftContext context, TimesheetDraftSnapshot snapshot)
    {
        List<AttendanceDay> attendanceDays = snapshot.Days.Select(day => new AttendanceDay(
            day.Date,
            day.ClockIn,
            day.ClockOut,
            day.BreakStart,
            day.BreakEnd,
            day.Description,
            day.Schedules,
            day.IsHoliday,
            context.TotalWorkload)).ToList();

        AttendanceTimesheet attendance = new(
            context.Timesheet.Employee.PersonalNumber,
            context.Timesheet.Employee.FullName,
            context.TotalWorkload,
            context.Timesheet.Year,
            context.Timesheet.Month,
            attendanceDays);

        List<CombinedDay> combinedDays = snapshot.Days.Select(day =>
        {
            decimal worked = TimesheetLogic.CalculateWorkedHoursFromAttendance(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd);
            return new CombinedDay(
                day.Date,
                day.IsHoliday,
                context.TotalWorkload,
                context.CoreWorkload,
                worked,
                day.CoreHours,
                day.ProjectHours.Values.Sum(),
                TimesheetLogic.CalculateStagHours(day.Schedules),
                day.ClockIn is not null || day.ClockOut is not null,
                TimesheetInterruptions.SkipAllocationRules(day.Description));
        }).ToList();

        CombinedTimesheet combined = new(context.Timesheet.Year, context.Timesheet.Month, context.CoreWorkload, combinedDays);
        TimesheetReview review = new CombinedTimesheetReviewer().Review(combined, attendance);
        IReadOnlyList<TimesheetIssue> issues = review.Issues.ToArray();
        IReadOnlyList<DayIssue> dayIssues = review.DayIssues.ToArray();

        List<TimesheetDayEvaluation> days = snapshot.Days.Zip(combinedDays).Select(pair =>
        {
            (TimesheetDraftDayState day, CombinedDay combinedDay) = pair;
            bool businessTrip = TimesheetInterruptions.HasBusinessTripInterruption(day.Description);
            bool coreOnly = TimesheetInterruptions.HasCoreOnlyInterruption(day.Description);
            bool proportional = TimesheetInterruptions.HasProportionalInterruption(day.Description);
            decimal balance = combinedDay.SkipAllocationRules || !combinedDay.HasAttendanceFilled
                ? 0m
                : TimesheetLogic.Round(combinedDay.WorkedHours - combinedDay.AllocatedHours);

            return new TimesheetDayEvaluation(
                day.Date.Day,
                combinedDay.WorkedHours,
                TimesheetLogic.CalculateNightHours(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd),
                combinedDay.AllocatedHours,
                balance,
                businessTrip,
                coreOnly,
                proportional);
        }).ToList();

        int workdays = snapshot.Days.Count(day => TimesheetLogic.IsWorkday(day.Date, day.IsHoliday));
        List<TimesheetProjectTotal> projectTotals = snapshot.Projects.Select(project => new TimesheetProjectTotal(
            project.Id,
            TimesheetLogic.Normalize(snapshot.Days.Sum(day => day.ProjectHours.GetValueOrDefault(project.Id))),
            TimesheetLogic.Normalize(workdays * 8m * project.Workload))).ToList();

        TimesheetTotals totals = new(
            TimesheetLogic.Normalize(combinedDays.Sum(day => day.WorkedHours)),
            TimesheetLogic.Normalize(workdays * 8m * context.TotalWorkload),
            TimesheetLogic.Normalize(combinedDays.Sum(day => day.AllocatedHours)),
            TimesheetLogic.Normalize(snapshot.Days.Sum(day => day.CoreHours)),
            TimesheetLogic.Normalize(workdays * 8m * context.CoreWorkload),
            projectTotals);

        return new TimesheetEvaluation(review.HasErrors, issues, dayIssues, days, totals);
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

            project.LockedAt = update.LockedAt;
            project.LockedBy = update.LockedBy;
            project.UpdatedAt = DateTime.UtcNow;
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
