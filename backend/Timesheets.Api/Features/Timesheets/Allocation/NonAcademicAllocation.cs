namespace Timesheets.Api.Features.Timesheets.Allocation;

/// <summary>Generates attendance-tracking (non-academic) allocations from existing attendance only.</summary>
internal sealed class NonAcademicAllocation
{
    private readonly EditableTimesheet _sheet;
    private readonly decimal _totalWorkload;

    public NonAcademicAllocation(LoadedTimesheet loaded, EditableTimesheet sheet)
    {
        _sheet = sheet;
        _totalWorkload = loaded.TotalWorkload;
    }

    public void AllocateMonth()
    {
        ResetGeneratedAllocations();
        foreach (EditableTimesheetDay day in _sheet.Days.Where(day => day.HasLockedContractPartHours()))
        {
            DistributeLockedContractPartDay(day);
        }

        MonthlyTargets targets = MonthlyTargets.NonAcademicCapacityRemainders(_sheet, AvailableMonthCapacity());
        DayTargetFiller filler = new(_sheet.ContractParts, _totalWorkload, tracksAttendance: true, targets);
        foreach (EditableTimesheetDay day in _sheet.Days.Where(day => AvailableDayCapacity(day) > 0m))
        {
            filler.Fill(day);
        }
        ReconcileProjectRemainders();
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
            DistributeLockedContractPartDay(day);
            return;
        }

        day.ResetGeneratedAllocations(_sheet.ContractParts);
        MonthlyTargets targets = MonthlyTargets.NonAcademicCapacityRemainders(_sheet, AvailableMonthCapacity());
        new DayTargetFiller(_sheet.ContractParts, _totalWorkload, tracksAttendance: true, targets).Fill(day);
    }

    private void ResetGeneratedAllocations()
    {
        foreach (EditableTimesheetDay day in _sheet.Days)
        {
            if (!day.CoreHoursFixed)
            {
                day.CoreHours = 0m;
            }

            foreach (ContractPartColumn project in _sheet.ContractParts)
            {
                if (!day.ContractPartHoursFixed.GetValueOrDefault(project.Id))
                {
                    day.ContractPartHours[project.Id] = day.ProjectFloor(project.Id);
                }
            }
        }
    }

    private void DistributeLockedContractPartDay(EditableTimesheetDay day)
    {
        foreach (ContractPartColumn project in _sheet.ContractParts)
        {
            if (!day.ContractPartHoursFixed.GetValueOrDefault(project.Id))
            {
                day.ContractPartHours[project.Id] = day.ProjectFloor(project.Id);
            }
        }

        if (!day.CoreHoursFixed)
        {
            decimal projectHours = TimesheetEvaluator.Normalize(_sheet.ContractParts.Sum(project => day.ContractPartHours.GetValueOrDefault(project.Id)));
            day.CoreHours = TimesheetEvaluator.Normalize(Math.Max(0m, AvailableDayCapacity(day) - projectHours));
        }
    }

    private decimal AvailableMonthCapacity() =>
        TimesheetEvaluator.Normalize(_sheet.Days.Sum(AvailableDayCapacity));

    private decimal AvailableDayCapacity(EditableTimesheetDay day) =>
        TimesheetEvaluator.HasBusinessTripInterruption(day.Description)
            ? 0m
            : TimesheetEvaluator.DayCapacity(day.Date, day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd, day.Description, _totalWorkload, tracksAttendance: true, day.Schedules);

    private void ReconcileProjectRemainders()
    {
        foreach (ContractPartColumn project in _sheet.ContractParts.Where(project => !project.Locked).OrderBy(_ => Random.Shared.Next()))
        {
            decimal target = MonthlyTargets.ContractPartTarget(_sheet, project);
            decimal missing = TimesheetEvaluator.Normalize(target - _sheet.Days.Sum(day => day.ContractPartHours.GetValueOrDefault(project.Id)));
            if (missing <= 0m)
            {
                continue;
            }

            foreach (EditableTimesheetDay day in AdjustableDays(project))
            {
                decimal amount = Math.Min(missing, AdjustmentCapacity(day));
                if (amount <= 0m)
                {
                    continue;
                }

                decimal fromCore = day.CoreHoursFixed ? 0m : Math.Min(day.CoreHours, amount);
                day.CoreHours = TimesheetEvaluator.Normalize(day.CoreHours - fromCore);
                day.ContractPartHours[project.Id] = TimesheetEvaluator.Normalize(day.ContractPartHours.GetValueOrDefault(project.Id) + amount);
                missing = TimesheetEvaluator.Normalize(missing - amount);
                if (missing <= 0m)
                {
                    break;
                }
            }
        }
    }

    private IEnumerable<EditableTimesheetDay> AdjustableDays(ContractPartColumn project) =>
        _sheet.Days
            .Where(day =>
                project.IsActiveOn(day.Date) &&
                !day.ContractPartHoursFixed.GetValueOrDefault(project.Id) &&
                !TimesheetEvaluator.HasBusinessTripInterruption(day.Description) &&
                !TimesheetEvaluator.HasProportionalInterruption(day.Description) &&
                AdjustmentCapacity(day) > 0m)
            .OrderByDescending(day => day.ContractPartHours.GetValueOrDefault(project.Id) > 0m)
            .ThenBy(_ => Random.Shared.Next());

    private decimal AdjustmentCapacity(EditableTimesheetDay day)
    {
        decimal free = TimesheetEvaluator.Normalize(AvailableDayCapacity(day) - day.TotalHours());
        decimal core = day.CoreHoursFixed ? 0m : day.CoreHours;
        return TimesheetEvaluator.Normalize(Math.Max(0m, free) + core);
    }
}
