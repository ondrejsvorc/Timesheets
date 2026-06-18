using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Timesheets.Api.Auth;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Employees;
using Timesheets.Api.Timesheets;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class AllocateTimesheet : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/{id}/allocate", Handle)
            .WithSummary("Allocate Timesheet Edit")
            .WithRequestValidation<TimesheetEditRequest>();

    public sealed record DayResponse(DateTime Date, int?[] Work, int?[] Break, decimal CoreHours, IReadOnlyDictionary<Guid, decimal> ProjectHours);
    public sealed record Response(IReadOnlyList<DayResponse> Days, TimesheetEvaluation Evaluation);

    private static async Task<Results<Ok<Response>, NotFound, ForbidHttpResult>> Handle(Guid id, [FromQuery] int? day, [FromBody] TimesheetEditRequest request, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        LoadedTimesheet? loaded = await TimesheetEngine.LoadAsync(id, dbContext, cancellationToken);
        if (loaded is null)
        {
            return TypedResults.NotFound();
        }
        if ((!user.IsGlobalManagerRole() && user.EmployeeId != loaded.Timesheet.EmployeeId) || loaded.Timesheet.TimesheetStatusId != TimesheetWorkflow.DraftStatusId)
        {
            return TypedResults.Forbid();
        }

        return TypedResults.Ok(Allocate(loaded, request, day));
    }

    private static Response Allocate(LoadedTimesheet loaded, TimesheetEditRequest request, int? dayNumber)
    {
        EditableTimesheet sheet = TimesheetEngine.BuildEditableTimesheet(loaded, request);
        bool tracksAttendance = EmployeeTypes.TracksAttendance(loaded.Timesheet.Employee.EmployeeTypeId);
        if (!tracksAttendance && !dayNumber.HasValue)
        {
            AllocateAcademicMonth(loaded, sheet);
            return ToAllocation(loaded, sheet);
        }

        IReadOnlyList<EditableTimesheetDay> days = dayNumber.HasValue ? sheet.Days.Where(day => day.Date.Day == dayNumber.Value).ToArray() : OrderDays(sheet.Days);
        Dictionary<Guid, decimal> targets = sheet.Projects.ToDictionary(project => project.Id, project => Math.Max(0m, MonthlyTarget(sheet, project) - sheet.Days.Sum(value => value.ProjectHours.GetValueOrDefault(project.Id))));
        decimal coreTarget = Math.Max(0m, CoreMonthlyTarget(sheet, loaded.TotalWorkload) - sheet.Days.Sum(value => value.CoreHours));

        foreach (EditableTimesheetDay day in days)
        {
            GenerateAttendanceIfMissing(day, tracksAttendance);
            AllocateDay(day, sheet.Projects, loaded.TotalWorkload, tracksAttendance, ref coreTarget, targets);
        }

        return ToAllocation(loaded, sheet);
    }

    private static Response ToAllocation(LoadedTimesheet loaded, EditableTimesheet sheet)
    {
        List<DayResponse> allocation = sheet.Days.Select(day => new DayResponse(Date: day.Date, Work: [ToMinutes(day.ClockIn), ToMinutes(day.ClockOut)], Break: [ToMinutes(day.BreakStart), ToMinutes(day.BreakEnd)], CoreHours: day.CoreHours, ProjectHours: day.ProjectHours)).ToList();
        return new Response(Days: allocation, Evaluation: TimesheetEngine.Evaluate(loaded, sheet));
    }

    private static void AllocateDay(EditableTimesheetDay day, IReadOnlyList<ProjectColumn> projects, decimal totalWorkload, bool tracksAttendance, ref decimal coreTarget, Dictionary<Guid, decimal> projectTargets)
    {
        if (TimesheetInterruptions.HasBusinessTripInterruption(day.Description))
        {
            return;
        }

        if (TimesheetInterruptions.HasProportionalInterruption(day.Description))
        {
            decimal previousCoreHours = day.CoreHours;
            Dictionary<Guid, decimal> previousProjectHours = projects.ToDictionary(project => project.Id, project => day.ProjectHours.GetValueOrDefault(project.Id));
            TimesheetInterruptionHours.ApplyToDayState(day, projects, totalWorkload, tracksAttendance);
            coreTarget = Math.Max(0m, coreTarget - (day.CoreHours - previousCoreHours));
            foreach (ProjectColumn project in projects)
            {
                projectTargets[project.Id] = Math.Max(0m, projectTargets[project.Id] - (day.ProjectHours.GetValueOrDefault(project.Id) - previousProjectHours[project.Id]));
            }

            return;
        }

        decimal capacity = TimesheetInterruptionHours.DayCapacity(day.Date, day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd, day.Description, totalWorkload, tracksAttendance, day.Schedules);
        decimal free = TimesheetLogic.Normalize(capacity - day.CoreHours - day.ProjectHours.Values.Sum());
        if (free <= 0)
        {
            return;
        }

        decimal stagHours = TimesheetLogic.CalculateStagHours(day.Schedules);
        decimal stagMissing = Math.Max(0m, stagHours - day.CoreHours);
        bool coreCanReceiveRemainder = !day.CoreHoursFixed && coreTarget > 0m;
        if (!day.CoreHoursFixed && stagMissing > 0)
        {
            decimal core = PreferQuarterStagTopUp(day.CoreHours, stagHours, free, coreTarget);
            day.CoreHours += core;
            coreTarget = Math.Max(0m, coreTarget - core);
            free -= core;
        }

        List<(Guid ProjectId, decimal Remaining)> projectRemaining = [];
        foreach (ProjectColumn project in projects)
        {
            bool fixedHours = day.ProjectHoursFixed.GetValueOrDefault(project.Id);
            if (project.IsActiveOn(day.Date) && !project.Locked && !fixedHours && day.ProjectHours.GetValueOrDefault(project.Id) == 0 && projectTargets[project.Id] > 0)
            {
                projectRemaining.Add((project.Id, projectTargets[project.Id]));
            }
        }

        decimal coreRemaining = coreCanReceiveRemainder ? coreTarget : 0m;
        decimal totalRemaining = coreRemaining + projectRemaining.Sum(item => item.Remaining);
        decimal amount = Math.Min(free, totalRemaining);
        decimal left = amount;

        foreach ((Guid projectId, decimal target) in projectRemaining)
        {
            decimal maxValue = Math.Min(target, left);
            decimal value = PreferQuarter(TimesheetLogic.Normalize(amount * target / totalRemaining), maxValue);
            left -= value;
            day.ProjectHours[projectId] = value;
            projectTargets[projectId] = Math.Max(0m, projectTargets[projectId] - value);
        }

        if (coreRemaining > 0m && left > 0m)
        {
            day.CoreHours += left;
            coreTarget = Math.Max(0m, coreTarget - left);
            left = 0m;
        }

        foreach ((Guid projectId, _) in projectRemaining)
        {
            if (left <= 0m)
            {
                break;
            }

            decimal value = Math.Min(projectTargets[projectId], left);
            day.ProjectHours[projectId] += value;
            projectTargets[projectId] = Math.Max(0m, projectTargets[projectId] - value);
            left -= value;
        }
    }

    private static void AllocateAcademicMonth(LoadedTimesheet loaded, EditableTimesheet sheet)
    {
        foreach (EditableTimesheetDay day in sheet.Days.Where(day => TimesheetInterruptions.SkipAllocationRules(day.Description)))
        {
            TimesheetInterruptionHours.ApplyToDayState(day, sheet.Projects, loaded.TotalWorkload, tracksAttendance: false);
        }

        ApplyStagMinimums(sheet);

        decimal coreTarget = Math.Max(0m, CoreMonthlyTarget(sheet, loaded.TotalWorkload) - sheet.Days.Sum(day => day.CoreHours));
        Dictionary<Guid, decimal> projectTargets = sheet.Projects.ToDictionary(project => project.Id, project => Math.Max(0m, MonthlyTarget(sheet, project) - sheet.Days.Sum(day => day.ProjectHours.GetValueOrDefault(project.Id))));
        decimal remaining = TimesheetLogic.Normalize(coreTarget + projectTargets.Values.Sum());
        if (remaining <= 0m)
        {
            return;
        }

        List<EditableTimesheetDay> candidates = sheet.Days
            .Where(day => IsAcademicAllocationDay(day) && !TimesheetInterruptions.SkipAllocationRules(day.Description) && CanReceiveAnyHours(day, sheet.Projects))
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        List<EditableTimesheetDay> activeDays = candidates.Where(day => DayTotal(day) > 0m).ToList();
        int activeCount = ChooseActiveDayCount(candidates.Count, activeDays.Count, candidates.Sum(DayTotal) + remaining);
        foreach (EditableTimesheetDay day in candidates.Where(day => DayTotal(day) == 0m).OrderBy(_ => Random.Shared.Next()).Take(Math.Max(0, activeCount - activeDays.Count)))
        {
            activeDays.Add(day);
        }
        AddCapacityDays(activeDays, candidates, remaining);

        Dictionary<EditableTimesheetDay, decimal> dayTargets = BuildDayTargets(activeDays, remaining);
        AllocateIntoDayTargets(dayTargets, sheet.Projects, ref coreTarget, projectTargets);
    }

    private static void AddCapacityDays(List<EditableTimesheetDay> activeDays, IReadOnlyList<EditableTimesheetDay> candidates, decimal remaining)
    {
        decimal neededCapacity = TimesheetLogic.Normalize(activeDays.Sum(DayTotal) + remaining);
        foreach (EditableTimesheetDay day in candidates.Except(activeDays).OrderBy(_ => Random.Shared.Next()))
        {
            if (activeDays.Sum(AcademicDayMaxHours) >= neededCapacity)
            {
                return;
            }

            activeDays.Add(day);
        }
    }

    private static void ApplyStagMinimums(EditableTimesheet sheet)
    {
        foreach (EditableTimesheetDay day in sheet.Days.Where(day => !day.CoreHoursFixed && !TimesheetInterruptions.SkipAllocationRules(day.Description)))
        {
            decimal required = TimesheetLogic.Normalize(TimesheetLogic.CalculateStagHours(day.Schedules) - day.CoreHours);
            if (required > 0m)
            {
                day.CoreHours = TimesheetLogic.Normalize(day.CoreHours + required);
            }
        }
    }

    private static Dictionary<EditableTimesheetDay, decimal> BuildDayTargets(IReadOnlyList<EditableTimesheetDay> days, decimal remaining)
    {
        Dictionary<EditableTimesheetDay, decimal> targets = days.ToDictionary(day => day, DayTotal);
        decimal finalTotal = TimesheetLogic.Normalize(targets.Values.Sum() + remaining);
        if (finalTotal >= 6m)
        {
            foreach (EditableTimesheetDay day in days.OrderBy(_ => Random.Shared.Next()))
            {
                decimal dayMax = AcademicDayMaxHours(day);
                decimal add = Math.Min(Math.Min(6m - targets[day], dayMax - targets[day]), remaining);
                if (add > 0m)
                {
                    targets[day] = TimesheetLogic.Normalize(targets[day] + add);
                    remaining = TimesheetLogic.Normalize(remaining - add);
                }
            }
        }

        foreach (EditableTimesheetDay day in days.OrderBy(_ => Random.Shared.Next()))
        {
            decimal dayMax = AcademicDayMaxHours(day);
            decimal add = Math.Min(Math.Min(RandomDayHours() - targets[day], dayMax - targets[day]), remaining);
            if (add > 0m)
            {
                targets[day] = TimesheetLogic.Normalize(targets[day] + add);
                remaining = TimesheetLogic.Normalize(remaining - add);
            }
        }

        while (remaining > 0m)
        {
            List<EditableTimesheetDay> available = days.Where(day => targets[day] < AcademicDayMaxHours(day)).ToList();
            if (available.Count == 0)
            {
                break;
            }

            EditableTimesheetDay day = available[Random.Shared.Next(available.Count)];
            decimal dayMax = AcademicDayMaxHours(day);
            decimal add = Math.Min(Math.Min(RandomAmount(dayMax - targets[day]), dayMax - targets[day]), remaining);
            targets[day] = TimesheetLogic.Normalize(targets[day] + add);
            remaining = TimesheetLogic.Normalize(remaining - add);
        }

        return targets;
    }

    private static bool IsAcademicAllocationDay(EditableTimesheetDay day) =>
        TimesheetLogic.IsWeekday(day.Date) || TimesheetLogic.CalculateStagHours(day.Schedules) > 0m;

    private static decimal AcademicDayMaxHours(EditableTimesheetDay day)
    {
        if (TimesheetLogic.IsWeekday(day.Date) || !string.IsNullOrWhiteSpace(day.Description))
        {
            return 12m;
        }

        decimal stagHours = TimesheetLogic.CalculateStagHours(day.Schedules);
        return stagHours > 0m ? TimesheetLogic.Normalize(Math.Min(12m, stagHours)) : 0m;
    }

    private static void AllocateIntoDayTargets(Dictionary<EditableTimesheetDay, decimal> dayTargets, IReadOnlyList<ProjectColumn> projects, ref decimal coreTarget, Dictionary<Guid, decimal> projectTargets)
    {
        Dictionary<Guid, ProjectColumn> projectsById = projects.ToDictionary(project => project.Id);
        while (coreTarget + projectTargets.Values.Sum() > 0m)
        {
            List<(EditableTimesheetDay Day, Guid? ProjectId, decimal Gap, decimal Remaining)> options = [];
            foreach ((EditableTimesheetDay day, decimal target) in dayTargets)
            {
                decimal gap = TimesheetLogic.Normalize(target - DayTotal(day));
                if (gap <= 0m)
                {
                    continue;
                }

                if (!day.CoreHoursFixed && coreTarget > 0m)
                {
                    options.Add((day, null, gap, coreTarget));
                }

                foreach ((Guid projectId, decimal targetLeft) in projectTargets)
                {
                    if (targetLeft > 0m && projectsById[projectId].IsActiveOn(day.Date) && !projectsById[projectId].Locked && !day.ProjectHoursFixed.GetValueOrDefault(projectId))
                    {
                        options.Add((day, projectId, gap, targetLeft));
                    }
                }
            }

            if (options.Count == 0)
            {
                break;
            }

            (EditableTimesheetDay selectedDay, Guid? selectedProjectId, decimal selectedGap, decimal selectedTargetLeft) = options[Random.Shared.Next(options.Count)];
            decimal amount = Math.Min(Math.Min(RandomAmount(selectedGap), selectedGap), selectedTargetLeft);
            if (selectedProjectId is null)
            {
                selectedDay.CoreHours = TimesheetLogic.Normalize(selectedDay.CoreHours + amount);
                coreTarget = TimesheetLogic.Normalize(coreTarget - amount);
            }
            else
            {
                selectedDay.ProjectHours[selectedProjectId.Value] = TimesheetLogic.Normalize(selectedDay.ProjectHours.GetValueOrDefault(selectedProjectId.Value) + amount);
                projectTargets[selectedProjectId.Value] = TimesheetLogic.Normalize(projectTargets[selectedProjectId.Value] - amount);
            }
        }
    }

    private static int ChooseActiveDayCount(int candidatesCount, int activeCount, decimal totalHours)
    {
        int lower = Math.Max(activeCount, (int)Math.Ceiling(totalHours / 12m));
        int upper = Math.Min(candidatesCount, (int)Math.Floor(totalHours / 6m));
        if (activeCount < candidatesCount && totalHours <= 12m * (candidatesCount - 1))
        {
            upper = Math.Min(upper, candidatesCount - 1);
        }

        if (upper < lower)
        {
            upper = lower;
        }

        int preferred = (int)Math.Round(totalHours / RandomDayHours(), MidpointRounding.AwayFromZero) + Random.Shared.Next(-2, 3);
        return Math.Min(candidatesCount, Math.Clamp(preferred, lower, upper));
    }

    private static bool CanReceiveAnyHours(EditableTimesheetDay day, IReadOnlyList<ProjectColumn> projects) =>
        !day.CoreHoursFixed || projects.Any(project => project.IsActiveOn(day.Date) && !project.Locked && !day.ProjectHoursFixed.GetValueOrDefault(project.Id));

    private static decimal DayTotal(EditableTimesheetDay day) => TimesheetLogic.Normalize(day.CoreHours + day.ProjectHours.Values.Sum());

    private static decimal RandomDayHours()
    {
        decimal raw = Random.Shared.NextDouble() < 0.7
            ? 7m + (decimal)Random.Shared.NextDouble() * 2m
            : 6m + (decimal)Random.Shared.NextDouble() * 6m;
        return Math.Min(12m, Math.Max(6m, HumanizeAmount(raw)));
    }

    private static decimal RandomAmount(decimal max)
    {
        if (max <= 0.25m)
        {
            return TimesheetLogic.Normalize(max);
        }

        decimal raw = 0.25m + (decimal)Random.Shared.NextDouble() * (max - 0.25m);
        return Math.Min(max, HumanizeAmount(raw));
    }

    private static decimal HumanizeAmount(decimal raw)
    {
        double mode = Random.Shared.NextDouble();
        if (mode < 0.6)
        {
            return RoundToQuarter(raw);
        }

        if (mode < 0.85)
        {
            return TimesheetLogic.Normalize(Math.Round(raw * 10m, MidpointRounding.AwayFromZero) / 10m);
        }

        return TimesheetLogic.Normalize(raw);
    }

    private static void GenerateAttendanceIfMissing(EditableTimesheetDay day, bool tracksAttendance)
    {
        if (!tracksAttendance
            || TimesheetInterruptions.SkipAllocationRules(day.Description)
            || day.ClockIn is not null
            || day.ClockOut is not null
            || day.BreakStart is not null
            || day.BreakEnd is not null)
        {
            return;
        }

        decimal allocated = TimesheetLogic.Normalize(day.CoreHours + day.ProjectHours.Values.Sum());
        decimal stag = TimesheetLogic.CalculateStagHours(day.Schedules);
        decimal work = Math.Max(allocated, stag);
        if (work <= 0m)
        {
            return;
        }

        TimeSpan start = day.Schedules.Count > 0 ? day.Schedules.Min(schedule => schedule.Start) : new TimeSpan(7, 0, 0);
        int workMinutes = (int)Math.Round(work * 60m, MidpointRounding.AwayFromZero);
        bool needsBreak = work > 6m;
        int breakMinutes = needsBreak ? 30 : 0;
        TimeSpan end = start.Add(TimeSpan.FromMinutes(workMinutes + breakMinutes));
        if (end >= TimeSpan.FromDays(1))
        {
            return;
        }

        day.ClockIn = start;
        day.ClockOut = end;
        if (needsBreak)
        {
            day.BreakStart = start.Add(TimeSpan.FromHours(4));
            day.BreakEnd = day.BreakStart.Value.Add(TimeSpan.FromMinutes(30));
        }
    }

    private static int? ToMinutes(TimeSpan? value) => value.HasValue ? (int)Math.Round(value.Value.TotalMinutes) : null;

    private static decimal MonthlyTarget(EditableTimesheet sheet, ProjectColumn project)
    {
        int fundedDays = sheet.Days.Count(day => TimesheetLogic.IsWeekday(day.Date) && project.IsActiveOn(day.Date));
        return TimesheetLogic.Normalize(fundedDays * 8m * project.Workload);
    }

    private static decimal CoreMonthlyTarget(EditableTimesheet sheet, decimal totalWorkload)
    {
        int fundedDays = sheet.Days.Count(day => TimesheetLogic.IsWeekday(day.Date));
        decimal total = TimesheetLogic.Normalize(fundedDays * 8m * totalWorkload);
        return TimesheetLogic.Normalize(total - sheet.Projects.Sum(project => MonthlyTarget(sheet, project)));
    }

    private static decimal PreferQuarterStagTopUp(decimal currentCoreHours, decimal stagHours, decimal free, decimal coreTarget)
    {
        decimal required = TimesheetLogic.Normalize(stagHours - currentCoreHours);
        if (required <= 0m)
        {
            return 0m;
        }

        decimal roundedFinal = RoundUpToQuarter(stagHours);
        decimal rounded = TimesheetLogic.Normalize(Math.Max(required, roundedFinal - currentCoreHours));
        return rounded <= free && rounded <= Math.Max(required, coreTarget) ? rounded : Math.Min(required, free);
    }

    private static decimal PreferQuarter(decimal value, decimal max)
    {
        decimal rounded = RoundToQuarter(value);
        if (rounded > max)
        {
            rounded = Math.Floor(max * 4m) / 4m;
        }

        return rounded > 0m ? rounded : Math.Min(value, max);
    }

    private static decimal RoundToQuarter(decimal value) => TimesheetLogic.Normalize(Math.Round(value * 4m, MidpointRounding.AwayFromZero) / 4m);
    private static decimal RoundUpToQuarter(decimal value) => TimesheetLogic.Normalize(Math.Ceiling(value * 4m) / 4m);

    private static IReadOnlyList<EditableTimesheetDay> OrderDays(IReadOnlyList<EditableTimesheetDay> days) => days
        .OrderByDescending(day => !string.IsNullOrWhiteSpace(day.Description))
        .ThenByDescending(day => TimesheetLogic.CalculateStagHours(day.Schedules) > day.CoreHours)
        .ThenByDescending(day => day.ClockIn is not null && day.ClockOut is not null)
        .ThenBy(day => day.Date)
        .ToArray();
}
