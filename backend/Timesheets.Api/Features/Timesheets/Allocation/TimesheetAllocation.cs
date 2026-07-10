namespace Timesheets.Api.Features.Timesheets.Allocation;

internal static class AllocationDayExtensions
{
    public const decimal CoreToleranceHours = 2m;

    public static decimal TotalHours(this EditableTimesheetDay day) => TimesheetLogic.Normalize(day.CoreHours + day.ProjectHours.Values.Sum());

    public static bool HasLockedProjectHours(this EditableTimesheetDay day) =>
        day.ProjectHoursFixed.Any(item => item.Value && day.ProjectHours.GetValueOrDefault(item.Key) > 0m);

    public static void ResetGeneratedAllocations(this EditableTimesheetDay day, IReadOnlyList<ProjectColumn> projects)
    {
        if (!day.CoreHoursFixed)
        {
            day.CoreHours = 0m;
        }

        foreach (ProjectColumn project in projects)
        {
            if (!day.ProjectHoursFixed.GetValueOrDefault(project.Id))
            {
                day.ProjectHours[project.Id] = day.ProjectFloor(project.Id);
            }
        }
    }

    public static decimal ProjectFloor(this EditableTimesheetDay day, Guid projectId) =>
        day.ProjectHoursFloor.GetValueOrDefault(projectId);

    public static void SetProjectHours(this EditableTimesheetDay day, Guid projectId, decimal hours)
    {
        decimal floor = day.ProjectFloor(projectId);
        day.ProjectHours[projectId] = HumanHours.RoundToHalfHour(Math.Max(floor, hours));
    }

    public static void AddProjectHours(this EditableTimesheetDay day, Guid projectId, decimal amount)
    {
        decimal current = day.ProjectHours.GetValueOrDefault(projectId);
        day.SetProjectHours(projectId, current + amount);
    }

    public static decimal WorkedHours(this EditableTimesheetDay day) =>
        TimesheetLogic.CalculateWorkedHoursFromAttendance(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd);
}

/// <summary>Monthly allocation goals and the hours still missing towards them.</summary>
internal sealed class MonthlyTargets
{
    private decimal _core;
    private readonly Dictionary<Guid, decimal> _projects;

    private MonthlyTargets(decimal core, Dictionary<Guid, decimal> projects)
    {
        _core = core;
        _projects = projects;
    }

    public decimal Core => _core;
    public IEnumerable<KeyValuePair<Guid, decimal>> Projects => _projects;
    public decimal Remaining => TimesheetLogic.Normalize(_core + _projects.Values.Sum());

    public decimal Project(Guid id) => _projects[id];

    public void ConsumeCore(decimal amount) => _core = TimesheetLogic.Normalize(Math.Max(0m, _core - amount));

    public void ConsumeProject(Guid id, decimal amount) => _projects[id] = TimesheetLogic.Normalize(Math.Max(0m, _projects[id] - amount));

    public static MonthlyTargets Remainders(EditableTimesheet sheet, decimal totalWorkload) => new(
        TimesheetLogic.Normalize(Math.Max(0m, CoreTarget(sheet, totalWorkload) - sheet.Days.Sum(day => day.CoreHours))),
        sheet.Projects.ToDictionary(
            project => project.Id,
            project => TimesheetLogic.Normalize(Math.Max(0m, ProjectTarget(sheet, project) - sheet.Days.Sum(day => day.ProjectHours.GetValueOrDefault(project.Id))))));

    public static MonthlyTargets NonAcademicCapacityRemainders(EditableTimesheet sheet, decimal availableHours)
    {
        Dictionary<Guid, decimal> projectHours = sheet.Projects.ToDictionary(
            project => project.Id,
            project => TimesheetLogic.Normalize(sheet.Days.Sum(day => day.ProjectHours.GetValueOrDefault(project.Id))));
        Dictionary<Guid, decimal> projectTargets = sheet.Projects.ToDictionary(
            project => project.Id,
            project => Math.Max(NonAcademicProjectTarget(sheet, project), projectHours[project.Id]));
        decimal coreTarget = TimesheetLogic.Normalize(Math.Max(0m, availableHours - projectTargets.Values.Sum()));

        return new MonthlyTargets(
            TimesheetLogic.Normalize(Math.Max(0m, coreTarget - sheet.Days.Sum(day => day.CoreHours))),
            projectTargets.ToDictionary(
                item => item.Key,
                item => TimesheetLogic.Normalize(Math.Max(0m, item.Value - projectHours[item.Key]))));
    }

    public static decimal NonAcademicProjectTarget(EditableTimesheet sheet, ProjectColumn project) =>
        ProjectTarget(sheet, project);

    public static decimal ProjectTarget(EditableTimesheet sheet, ProjectColumn project)
    {
        int fundedDays = sheet.Days.Count(day => TimesheetLogic.IsWorkday(day.Date, day.IsHoliday) && project.IsActiveOn(day.Date));
        return TimesheetLogic.Normalize(fundedDays * 8m * project.Workload);
    }

    public static decimal CoreTarget(EditableTimesheet sheet, decimal totalWorkload)
    {
        int fundedDays = sheet.Days.Count(day => TimesheetLogic.IsWorkday(day.Date, day.IsHoliday));
        decimal total = TimesheetLogic.Normalize(fundedDays * 8m * totalWorkload);
        return TimesheetLogic.Normalize(total - sheet.Projects.Sum(project => ProjectTarget(sheet, project)));
    }

    public static void AppendMismatch(List<string> errors, string label, decimal actual, decimal expected)
    {
        if (TimesheetLogic.HasUnequalHours(actual, expected))
        {
            errors.Add($"{label} {actual:F2}/{expected:F2}");
        }
    }
}

/// <summary>Randomness that makes generated timesheets look human-entered.</summary>
internal sealed class HumanHours
{
    public decimal RandomDayHours()
    {
        decimal raw = Random.Shared.NextDouble() < 0.7
            ? 7m + (decimal)Random.Shared.NextDouble() * 2m
            : 6m + (decimal)Random.Shared.NextDouble() * 6m;
        return Math.Min(12m, Math.Max(6m, Humanize(raw)));
    }

    public decimal RandomAmount(decimal max)
    {
        max = TimesheetLogic.Normalize(max);
        if (max <= 1m)
        {
            return max;
        }

        decimal raw = 1m + (decimal)Random.Shared.NextDouble() * (max - 1m);
        decimal amount = Math.Min(max, Humanize(raw));
        return max - amount is > 0m and < 1m ? max : amount;
    }

    private static decimal Humanize(decimal raw)
    {
        double mode = Random.Shared.NextDouble();
        if (mode < 0.8)
        {
            return RoundToHalfHour(raw);
        }

        if (mode < 0.95)
        {
            return RoundToQuarter(raw);
        }

        if (mode < 0.99)
        {
            return TimesheetLogic.Normalize(Math.Round(raw * 10m, MidpointRounding.AwayFromZero) / 10m);
        }

        return TimesheetLogic.Normalize(raw);
    }

    public static decimal RoundToHalfHour(decimal value) => TimesheetLogic.Normalize(Math.Round(value * 2m, MidpointRounding.AwayFromZero) / 2m);
    public static decimal RoundToQuarter(decimal value) => TimesheetLogic.Normalize(Math.Round(value * 4m, MidpointRounding.AwayFromZero) / 4m);
    public static decimal RoundUpToHalfHour(decimal value) => TimesheetLogic.Normalize(Math.Ceiling(value * 2m) / 2m);
}

/// <summary>Fills a single day's core and project hours from the monthly remainders.</summary>
internal sealed class DayTargetFiller(IReadOnlyList<ProjectColumn> projects, decimal totalWorkload, bool tracksAttendance, MonthlyTargets targets)
{
    public void Fill(EditableTimesheetDay day)
    {
        if (TimesheetInterruptions.HasBusinessTripInterruption(day.Description))
        {
            return;
        }
        if (day.HasLockedProjectHours())
        {
            return;
        }

        if (TimesheetInterruptions.HasProportionalInterruption(day.Description))
        {
            decimal previousCoreHours = day.CoreHours;
            Dictionary<Guid, decimal> previousProjectHours = projects.ToDictionary(project => project.Id, project => day.ProjectHours.GetValueOrDefault(project.Id));
            TimesheetInterruptionHours.ApplyToDayState(day, projects, totalWorkload, tracksAttendance);
            targets.ConsumeCore(day.CoreHours - previousCoreHours);
            foreach (ProjectColumn project in projects)
            {
                targets.ConsumeProject(project.Id, day.ProjectHours.GetValueOrDefault(project.Id) - previousProjectHours[project.Id]);
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
        bool coreCanReceiveRemainder = !day.CoreHoursFixed && targets.Core > 0m;
        if (!day.CoreHoursFixed && stagMissing > 0)
        {
            decimal core = PreferStagTopUpHours(day.CoreHours, stagHours, free, targets.Core);
            day.CoreHours += core;
            targets.ConsumeCore(core);
            free -= core;
        }

        List<(Guid ProjectId, decimal Remaining)> projectRemaining = [];
        foreach (ProjectColumn project in projects)
        {
            bool fixedHours = day.ProjectHoursFixed.GetValueOrDefault(project.Id);
            if (project.IsActiveOn(day.Date) && !project.Locked && !fixedHours && day.ProjectHours.GetValueOrDefault(project.Id) <= day.ProjectFloor(project.Id) && targets.Project(project.Id) > 0)
            {
                projectRemaining.Add((project.Id, targets.Project(project.Id)));
            }
        }

        decimal coreRemaining = coreCanReceiveRemainder ? targets.Core : 0m;
        decimal totalRemaining = coreRemaining + projectRemaining.Sum(item => item.Remaining);
        decimal amount = Math.Min(free, totalRemaining);
        decimal left = amount;

        foreach ((Guid projectId, decimal target) in projectRemaining)
        {
            decimal maxValue = Math.Min(target, left);
            decimal value = PreferGeneratedCellHours(TimesheetLogic.Normalize(amount * target / totalRemaining), maxValue);
            left -= value;
            day.SetProjectHours(projectId, value);
            targets.ConsumeProject(projectId, value);
        }

        if (!day.CoreHoursFixed && left > 0m)
        {
            day.CoreHours += left;
            targets.ConsumeCore(left);
            left = 0m;
        }
    }

    private static decimal PreferStagTopUpHours(decimal currentCoreHours, decimal stagHours, decimal free, decimal coreTarget)
    {
        decimal required = TimesheetLogic.Normalize(stagHours - currentCoreHours);
        if (required <= 0m)
        {
            return 0m;
        }

        decimal roundedFinal = HumanHours.RoundUpToHalfHour(stagHours);
        decimal rounded = TimesheetLogic.Normalize(Math.Max(required, roundedFinal - currentCoreHours));
        return rounded <= free && rounded <= Math.Max(required, coreTarget) ? rounded : Math.Min(required, free);
    }

    private static decimal PreferGeneratedCellHours(decimal value, decimal max)
    {
        max = TimesheetLogic.Normalize(max);
        if (max < 0.5m)
        {
            return 0m;
        }

        decimal rounded = Math.Max(0.5m, HumanHours.RoundToHalfHour(value));
        if (rounded > max)
        {
            rounded = Math.Floor(max * 2m) / 2m;
        }

        return rounded;
    }
}
