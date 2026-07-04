namespace Timesheets.Api.Timesheets.Allocation;

/// <summary>Generates academic timesheets: no attendance, randomized human-looking spread over weekdays and STAG days.</summary>
internal sealed class AcademicTimesheetAllocator
{
    private readonly EditableTimesheet _sheet;
    private readonly decimal _totalWorkload;
    private readonly HumanHours _humanHours = new();

    public AcademicTimesheetAllocator(LoadedTimesheet loaded, EditableTimesheet sheet)
    {
        _sheet = sheet;
        _totalWorkload = loaded.TotalWorkload;
    }

    public void AllocateMonth()
    {
        ApplyInterruptions();
        TopUpCoreToStag();

        MonthlyTargets targets = MonthlyTargets.Remainders(_sheet, _totalWorkload);
        if (targets.Remaining <= 0m)
        {
            return;
        }

        List<EditableTimesheetDay> candidates = _sheet.Days
            .Where(day =>
                CanAllocateDay(day) &&
                !TimesheetInterruptions.SkipAllocationRules(day.Description) &&
                !day.HasLockedProjectHours() &&
                CanDayReceiveHours(day))
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        List<EditableTimesheetDay> activeDays = SelectActiveDays(candidates, targets.Remaining);
        Dictionary<EditableTimesheetDay, decimal> dayTargets = DistributeDayTargets(activeDays, targets.Remaining);
        FillDayTargetsRandomly(dayTargets, targets);
        CompleteMonthlyTargets(activeDays, targets);
        EnsureMonthTargets();
    }

    public void AllocateDay(int dayNumber)
    {
        EditableTimesheetDay? day = _sheet.Days.SingleOrDefault(day => day.Date.Day == dayNumber);
        if (day is null)
        {
            return;
        }
        if (day.HasLockedProjectHours())
        {
            return;
        }

        MonthlyTargets targets = MonthlyTargets.Remainders(_sheet, _totalWorkload);
        new DayTargetFiller(_sheet.Projects, _totalWorkload, tracksAttendance: false, targets).Fill(day);
    }

    private void ApplyInterruptions()
    {
        foreach (EditableTimesheetDay day in _sheet.Days.Where(day => TimesheetInterruptions.SkipAllocationRules(day.Description)))
        {
            TimesheetInterruptionHours.ApplyToDayState(day, _sheet.Projects, _totalWorkload, tracksAttendance: false);
        }
    }

    private void TopUpCoreToStag()
    {
        foreach (EditableTimesheetDay day in _sheet.Days.Where(day => !day.CoreHoursFixed && !TimesheetInterruptions.SkipAllocationRules(day.Description) && !day.HasLockedProjectHours()))
        {
            decimal stagMissing = TimesheetLogic.Normalize(TimesheetLogic.CalculateStagHours(day.Schedules) - day.CoreHours);
            if (stagMissing > 0m)
            {
                day.CoreHours = TimesheetLogic.Normalize(day.CoreHours + stagMissing);
            }
        }
    }

    private List<EditableTimesheetDay> SelectActiveDays(List<EditableTimesheetDay> candidates, decimal remaining)
    {
        List<EditableTimesheetDay> activeDays = candidates.Where(day => day.TotalHours() > 0m).ToList();
        int activeCount = ChooseActiveDayCount(candidates.Count, activeDays.Count, candidates.Sum(day => day.TotalHours()) + remaining);
        foreach (EditableTimesheetDay day in candidates
            .Where(day => day.TotalHours() == 0m)
            .OrderBy(_ => Random.Shared.Next())
            .Take(Math.Max(0, activeCount - activeDays.Count)))
        {
            activeDays.Add(day);
        }

        decimal neededCapacity = TimesheetLogic.Normalize(activeDays.Sum(day => day.TotalHours()) + remaining);
        foreach (EditableTimesheetDay day in candidates.Except(activeDays).OrderBy(_ => Random.Shared.Next()))
        {
            if (activeDays.Sum(DayMaxHours) >= neededCapacity)
            {
                break;
            }

            activeDays.Add(day);
        }

        return activeDays;
    }

    private Dictionary<EditableTimesheetDay, decimal> DistributeDayTargets(List<EditableTimesheetDay> activeDays, decimal remaining)
    {
        Dictionary<EditableTimesheetDay, decimal> dayTargets = activeDays.ToDictionary(day => day, day => day.TotalHours());
        decimal finalTotal = TimesheetLogic.Normalize(dayTargets.Values.Sum() + remaining);
        if (finalTotal >= 6m)
        {
            foreach (EditableTimesheetDay day in activeDays.OrderBy(_ => Random.Shared.Next()))
            {
                decimal dayMax = DayMaxHours(day);
                decimal add = Math.Min(Math.Min(6m - dayTargets[day], dayMax - dayTargets[day]), remaining);
                if (add > 0m)
                {
                    dayTargets[day] = TimesheetLogic.Normalize(dayTargets[day] + add);
                    remaining = TimesheetLogic.Normalize(remaining - add);
                }
            }
        }

        foreach (EditableTimesheetDay day in activeDays.OrderBy(_ => Random.Shared.Next()))
        {
            decimal dayMax = DayMaxHours(day);
            decimal add = Math.Min(Math.Min(_humanHours.RandomDayHours() - dayTargets[day], dayMax - dayTargets[day]), remaining);
            if (add > 0m)
            {
                dayTargets[day] = TimesheetLogic.Normalize(dayTargets[day] + add);
                remaining = TimesheetLogic.Normalize(remaining - add);
            }
        }

        while (remaining > 0m)
        {
            List<EditableTimesheetDay> available = activeDays.Where(day => dayTargets[day] < DayMaxHours(day)).ToList();
            if (available.Count == 0)
            {
                break;
            }

            EditableTimesheetDay day = available[Random.Shared.Next(available.Count)];
            decimal dayMax = DayMaxHours(day);
            decimal add = Math.Min(Math.Min(_humanHours.RandomAmount(dayMax - dayTargets[day]), dayMax - dayTargets[day]), remaining);
            dayTargets[day] = TimesheetLogic.Normalize(dayTargets[day] + add);
            remaining = TimesheetLogic.Normalize(remaining - add);
        }

        return dayTargets;
    }

    private void FillDayTargetsRandomly(Dictionary<EditableTimesheetDay, decimal> dayTargets, MonthlyTargets targets)
    {
        Dictionary<Guid, ProjectColumn> projectsById = _sheet.Projects.ToDictionary(project => project.Id);
        while (targets.Remaining > 0m)
        {
            List<(EditableTimesheetDay Day, Guid? ProjectId, decimal Gap, decimal Remaining)> options = [];
            foreach ((EditableTimesheetDay day, decimal target) in dayTargets)
            {
                decimal gap = TimesheetLogic.Normalize(target - day.TotalHours());
                if (gap <= 0m)
                {
                    continue;
                }

                if (!day.CoreHoursFixed && targets.Core > 0m)
                {
                    options.Add((day, null, gap, targets.Core));
                }

                foreach ((Guid projectId, decimal targetLeft) in targets.Projects)
                {
                    if (targetLeft > 0m &&
                        projectsById[projectId].IsActiveOn(day.Date) &&
                        !projectsById[projectId].Locked &&
                        !day.ProjectHoursFixed.GetValueOrDefault(projectId))
                    {
                        options.Add((day, projectId, gap, targetLeft));
                    }
                }
            }

            if (options.Count == 0)
            {
                break;
            }

            List<(EditableTimesheetDay Day, Guid? ProjectId, decimal Gap, decimal Remaining)> nonTinyOptions = options
                .Where(option => option.Gap >= 1m && option.Remaining >= 1m)
                .ToList();
            if (nonTinyOptions.Count > 0)
            {
                options = nonTinyOptions;
            }
            else
            {
                break;
            }

            (EditableTimesheetDay selectedDay, Guid? selectedProjectId, decimal selectedGap, decimal selectedTargetLeft) =
                options[Random.Shared.Next(options.Count)];
            decimal maxAmount = Math.Min(selectedGap, selectedTargetLeft);
            decimal amount = _humanHours.RandomAmount(maxAmount);
            if (selectedProjectId is null)
            {
                selectedDay.CoreHours = TimesheetLogic.Normalize(selectedDay.CoreHours + amount);
                targets.ConsumeCore(amount);
            }
            else
            {
                selectedDay.ProjectHours[selectedProjectId.Value] = TimesheetLogic.Normalize(selectedDay.ProjectHours.GetValueOrDefault(selectedProjectId.Value) + amount);
                targets.ConsumeProject(selectedProjectId.Value, amount);
            }
        }
    }

    private void CompleteMonthlyTargets(IReadOnlyList<EditableTimesheetDay> days, MonthlyTargets targets)
    {
        foreach (ProjectColumn project in _sheet.Projects.OrderBy(_ => Random.Shared.Next()))
        {
            CompleteProjectTarget(days, project, targets, onlyExistingCells: true);
            CompleteProjectTarget(days, project, targets, onlyExistingCells: false);
            CompleteProjectTarget(days, project, targets, onlyExistingCells: true);
        }

        CompleteCoreTarget(days, targets, onlyExistingCells: true);
        CompleteCoreTarget(days, targets, onlyExistingCells: false);
        CompleteCoreTarget(days, targets, onlyExistingCells: true);
    }

    private void CompleteProjectTarget(IReadOnlyList<EditableTimesheetDay> days, ProjectColumn project, MonthlyTargets targets, bool onlyExistingCells)
    {
        foreach (EditableTimesheetDay day in days.OrderBy(_ => Random.Shared.Next()))
        {
            if (targets.Project(project.Id) <= 0m)
            {
                return;
            }
            if (!project.IsActiveOn(day.Date) || project.Locked || day.ProjectHoursFixed.GetValueOrDefault(project.Id))
            {
                continue;
            }

            decimal current = day.ProjectHours.GetValueOrDefault(project.Id);
            if (onlyExistingCells && current <= 0m)
            {
                continue;
            }

            decimal add = TimesheetLogic.Normalize(Math.Min(targets.Project(project.Id), FreeHours(day)));
            if (add <= 0m)
            {
                continue;
            }

            day.ProjectHours[project.Id] = TimesheetLogic.Normalize(current + add);
            targets.ConsumeProject(project.Id, add);
        }
    }

    private void CompleteCoreTarget(IReadOnlyList<EditableTimesheetDay> days, MonthlyTargets targets, bool onlyExistingCells)
    {
        foreach (EditableTimesheetDay day in days.OrderBy(_ => Random.Shared.Next()))
        {
            if (targets.Core <= 0m)
            {
                return;
            }
            if (day.CoreHoursFixed)
            {
                continue;
            }
            if (onlyExistingCells && day.CoreHours <= 0m)
            {
                continue;
            }

            decimal add = TimesheetLogic.Normalize(Math.Min(targets.Core, FreeHours(day)));
            if (add <= 0m)
            {
                continue;
            }

            day.CoreHours = TimesheetLogic.Normalize(day.CoreHours + add);
            targets.ConsumeCore(add);
        }
    }

    private void EnsureMonthTargets()
    {
        List<string> errors = [];
        MonthlyTargets.AppendMismatch(errors, "core", TimesheetLogic.Normalize(_sheet.Days.Sum(day => day.CoreHours)), MonthlyTargets.CoreTarget(_sheet, _totalWorkload));
        foreach (ProjectColumn project in _sheet.Projects)
        {
            MonthlyTargets.AppendMismatch(errors, $"project {project.Id}", TimesheetLogic.Normalize(_sheet.Days.Sum(day => day.ProjectHours.GetValueOrDefault(project.Id))), MonthlyTargets.ProjectTarget(_sheet, project));
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException("Generated academic timesheet missed targets: " + string.Join("; ", errors));
        }
    }

    private static bool CanAllocateDay(EditableTimesheetDay day) =>
        TimesheetLogic.IsWeekday(day.Date) || TimesheetLogic.CalculateStagHours(day.Schedules) > 0m;

    private static decimal DayMaxHours(EditableTimesheetDay day)
    {
        if (TimesheetLogic.IsWeekday(day.Date) || !string.IsNullOrWhiteSpace(day.Description))
        {
            return 12m;
        }

        decimal stagHours = TimesheetLogic.CalculateStagHours(day.Schedules);
        return stagHours > 0m ? TimesheetLogic.Normalize(Math.Min(12m, stagHours)) : 0m;
    }

    private static decimal FreeHours(EditableTimesheetDay day) =>
        TimesheetLogic.Normalize(DayMaxHours(day) - day.TotalHours());

    private int ChooseActiveDayCount(int candidatesCount, int activeCount, decimal totalHours)
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

        int preferred = (int)Math.Round(totalHours / _humanHours.RandomDayHours(), MidpointRounding.AwayFromZero) + Random.Shared.Next(-2, 3);
        return Math.Min(candidatesCount, Math.Clamp(preferred, lower, upper));
    }

    private bool CanDayReceiveHours(EditableTimesheetDay day) =>
        !day.CoreHoursFixed || _sheet.Projects.Any(project => project.IsActiveOn(day.Date) && !project.Locked && !day.ProjectHoursFixed.GetValueOrDefault(project.Id));
}
