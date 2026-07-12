namespace Timesheets.Api.Features.Timesheets.Allocation;

/// <summary>Generates academic timesheets: no attendance, randomized human-looking spread over weekdays and STAG days.</summary>
internal sealed class AcademicAllocation
{
    private readonly EditableTimesheet _sheet;
    private readonly decimal _totalWorkload;
    private readonly HumanHours _humanHours = new();

    public AcademicAllocation(LoadedTimesheet loaded, EditableTimesheet sheet)
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
                !TimesheetEvaluator.SkipAllocationRules(day.Description) &&
                !day.HasLockedContractPartHours() &&
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
        if (day.HasLockedContractPartHours())
        {
            return;
        }

        day.ResetGeneratedAllocations(_sheet.ContractParts);
        MonthlyTargets targets = MonthlyTargets.Remainders(_sheet, _totalWorkload);
        new DayTargetFiller(_sheet.ContractParts, _totalWorkload, tracksAttendance: false, targets).Fill(day);
    }

    private void ApplyInterruptions()
    {
        foreach (EditableTimesheetDay day in _sheet.Days.Where(day => TimesheetEvaluator.SkipAllocationRules(day.Description)))
        {
            TimesheetEvaluator.ApplyInterruptionToDayState(day, _sheet.ContractParts, _totalWorkload, tracksAttendance: false);
        }
    }

    private void TopUpCoreToStag()
    {
        foreach (EditableTimesheetDay day in _sheet.Days.Where(day => !day.CoreHoursFixed && !TimesheetEvaluator.SkipAllocationRules(day.Description) && !day.HasLockedContractPartHours()))
        {
            decimal stagMissing = TimesheetEvaluator.Normalize(TimesheetEvaluator.CalculateStagHours(day.Schedules) - day.CoreHours);
            if (stagMissing > 0m)
            {
                day.CoreHours = TimesheetEvaluator.Normalize(day.CoreHours + stagMissing);
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

        decimal neededCapacity = TimesheetEvaluator.Normalize(activeDays.Sum(day => day.TotalHours()) + remaining);
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
        decimal finalTotal = TimesheetEvaluator.Normalize(dayTargets.Values.Sum() + remaining);
        if (finalTotal >= 6m)
        {
            foreach (EditableTimesheetDay day in activeDays.OrderBy(_ => Random.Shared.Next()))
            {
                decimal dayMax = DayMaxHours(day);
                decimal add = Math.Min(Math.Min(6m - dayTargets[day], dayMax - dayTargets[day]), remaining);
                if (add > 0m)
                {
                    dayTargets[day] = TimesheetEvaluator.Normalize(dayTargets[day] + add);
                    remaining = TimesheetEvaluator.Normalize(remaining - add);
                }
            }
        }

        foreach (EditableTimesheetDay day in activeDays.OrderBy(_ => Random.Shared.Next()))
        {
            decimal dayMax = DayMaxHours(day);
            decimal add = Math.Min(Math.Min(_humanHours.RandomDayHours() - dayTargets[day], dayMax - dayTargets[day]), remaining);
            if (add > 0m)
            {
                dayTargets[day] = TimesheetEvaluator.Normalize(dayTargets[day] + add);
                remaining = TimesheetEvaluator.Normalize(remaining - add);
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
            dayTargets[day] = TimesheetEvaluator.Normalize(dayTargets[day] + add);
            remaining = TimesheetEvaluator.Normalize(remaining - add);
        }

        return dayTargets;
    }

    private void FillDayTargetsRandomly(Dictionary<EditableTimesheetDay, decimal> dayTargets, MonthlyTargets targets)
    {
        Dictionary<Guid, ContractPartColumn> projectsById = _sheet.ContractParts.ToDictionary(project => project.Id);
        while (targets.Remaining > 0m)
        {
            List<(EditableTimesheetDay Day, Guid? ProjectId, decimal Gap, decimal Remaining)> options = [];
            foreach ((EditableTimesheetDay day, decimal target) in dayTargets)
            {
                decimal gap = TimesheetEvaluator.Normalize(target - day.TotalHours());
                if (gap <= 0m)
                {
                    continue;
                }

                if (!day.CoreHoursFixed && targets.Core > 0m)
                {
                    options.Add((day, null, gap, targets.Core));
                }

                foreach ((Guid contractEmployeeId, decimal targetLeft) in targets.Projects)
                {
                    if (targetLeft > 0m &&
                        projectsById[contractEmployeeId].IsActiveOn(day.Date) &&
                        !projectsById[contractEmployeeId].Locked &&
                        !day.ContractPartHoursFixed.GetValueOrDefault(contractEmployeeId))
                    {
                        options.Add((day, contractEmployeeId, gap, targetLeft));
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
            else if (options.Any(HasExistingCellHours))
            {
                options = options.Where(HasExistingCellHours).ToList();
                List<(EditableTimesheetDay Day, Guid? ProjectId, decimal Gap, decimal Remaining)> lowDayOptions = options
                    .Where(option => option.Day.TotalHours() is > 0m and < 6m)
                    .ToList();
                if (lowDayOptions.Count > 0)
                {
                    options = lowDayOptions;
                }
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
                selectedDay.CoreHours = TimesheetEvaluator.Normalize(selectedDay.CoreHours + amount);
                targets.ConsumeCore(amount);
            }
            else
            {
                selectedDay.ContractPartHours[selectedProjectId.Value] = TimesheetEvaluator.Normalize(selectedDay.ContractPartHours.GetValueOrDefault(selectedProjectId.Value) + amount);
                targets.ConsumeProject(selectedProjectId.Value, amount);
            }
        }
    }

    private static bool HasExistingCellHours((EditableTimesheetDay Day, Guid? ProjectId, decimal Gap, decimal Remaining) option) =>
        option.ProjectId is Guid contractEmployeeId
            ? option.Day.ContractPartHours.GetValueOrDefault(contractEmployeeId) > 0m
            : option.Day.CoreHours > 0m;

    private void CompleteMonthlyTargets(IReadOnlyList<EditableTimesheetDay> days, MonthlyTargets targets)
    {
        foreach (ContractPartColumn project in _sheet.ContractParts.OrderBy(_ => Random.Shared.Next()))
        {
            CompleteContractPartTarget(days, project, targets, onlyExistingCells: true);
            CompleteContractPartTarget(days, project, targets, onlyExistingCells: false);
            CompleteContractPartTarget(days, project, targets, onlyExistingCells: true);
        }

        CompleteCoreTarget(days, targets, onlyExistingCells: true);
        CompleteCoreTarget(days, targets, onlyExistingCells: false);
        CompleteCoreTarget(days, targets, onlyExistingCells: true);
    }

    private void CompleteContractPartTarget(IReadOnlyList<EditableTimesheetDay> days, ContractPartColumn project, MonthlyTargets targets, bool onlyExistingCells)
    {
        foreach (EditableTimesheetDay day in days.OrderBy(_ => Random.Shared.Next()))
        {
            if (targets.Project(project.Id) <= 0m)
            {
                return;
            }
            if (!project.IsActiveOn(day.Date) || project.Locked || day.ContractPartHoursFixed.GetValueOrDefault(project.Id))
            {
                continue;
            }

            decimal current = day.ContractPartHours.GetValueOrDefault(project.Id);
            if (onlyExistingCells && current <= 0m)
            {
                continue;
            }

            decimal add = TimesheetEvaluator.Normalize(Math.Min(targets.Project(project.Id), FreeHours(day)));
            if (add <= 0m)
            {
                continue;
            }

            day.ContractPartHours[project.Id] = TimesheetEvaluator.Normalize(current + add);
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

            decimal add = TimesheetEvaluator.Normalize(Math.Min(targets.Core, FreeHours(day)));
            if (add <= 0m)
            {
                continue;
            }

            day.CoreHours = TimesheetEvaluator.Normalize(day.CoreHours + add);
            targets.ConsumeCore(add);
        }
    }

    private void EnsureMonthTargets()
    {
        List<string> errors = [];
        MonthlyTargets.AppendMismatch(errors, "core", TimesheetEvaluator.Normalize(_sheet.Days.Sum(day => day.CoreHours)), MonthlyTargets.CoreTarget(_sheet, _totalWorkload));
        foreach (ContractPartColumn project in _sheet.ContractParts)
        {
            MonthlyTargets.AppendMismatch(errors, $"project {project.Id}", TimesheetEvaluator.Normalize(_sheet.Days.Sum(day => day.ContractPartHours.GetValueOrDefault(project.Id))), MonthlyTargets.ContractPartTarget(_sheet, project));
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException("Generated academic timesheet missed targets: " + string.Join("; ", errors));
        }
    }

    private static bool CanAllocateDay(EditableTimesheetDay day) =>
        TimesheetEvaluator.IsWeekday(day.Date) || TimesheetEvaluator.CalculateStagHours(day.Schedules) > 0m;

    private static decimal DayMaxHours(EditableTimesheetDay day)
    {
        if (TimesheetEvaluator.IsWeekday(day.Date) || !string.IsNullOrWhiteSpace(day.Description))
        {
            return 12m;
        }

        decimal stagHours = TimesheetEvaluator.CalculateStagHours(day.Schedules);
        return stagHours > 0m ? TimesheetEvaluator.Normalize(Math.Min(12m, stagHours)) : 0m;
    }

    private static decimal FreeHours(EditableTimesheetDay day) =>
        TimesheetEvaluator.Normalize(DayMaxHours(day) - day.TotalHours());

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
        !day.CoreHoursFixed || _sheet.ContractParts.Any(project => project.IsActiveOn(day.Date) && !project.Locked && !day.ContractPartHoursFixed.GetValueOrDefault(project.Id));
}
