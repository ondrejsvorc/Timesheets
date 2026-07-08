namespace Timesheets.Api.Features.Timesheets.Allocation;

/// <summary>Generates attendance-tracking (non-academic) allocations from existing attendance only.</summary>
internal sealed class NonAcademicTimesheetAllocator
{
    private readonly EditableTimesheet _sheet;
    private readonly decimal _totalWorkload;

    public NonAcademicTimesheetAllocator(LoadedTimesheet loaded, EditableTimesheet sheet)
    {
        _sheet = sheet;
        _totalWorkload = loaded.TotalWorkload;
    }

    public void AllocateMonth()
    {
        ResetGeneratedAllocations();
        foreach (EditableTimesheetDay day in _sheet.Days.Where(day => day.HasLockedProjectHours()))
        {
            DistributeLockedProjectDay(day);
        }

        MonthlyTargets targets = MonthlyTargets.NonAcademicCapacityRemainders(_sheet, AvailableMonthCapacity());
        DayTargetFiller filler = new(_sheet.Projects, _totalWorkload, tracksAttendance: true, targets);
        foreach (EditableTimesheetDay day in _sheet.Days.Where(day => AvailableDayCapacity(day) > 0m))
        {
            filler.Fill(day);
        }
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
            DistributeLockedProjectDay(day);
            return;
        }

        MonthlyTargets targets = MonthlyTargets.NonAcademicCapacityRemainders(_sheet, AvailableMonthCapacity());
        new DayTargetFiller(_sheet.Projects, _totalWorkload, tracksAttendance: true, targets).Fill(day);
    }

    private void ResetGeneratedAllocations()
    {
        foreach (EditableTimesheetDay day in _sheet.Days)
        {
            if (!day.CoreHoursFixed)
            {
                day.CoreHours = 0m;
            }

            foreach (ProjectColumn project in _sheet.Projects)
            {
                if (!day.ProjectHoursFixed.GetValueOrDefault(project.Id))
                {
                    day.ProjectHours[project.Id] = day.ProjectFloor(project.Id);
                }
            }
        }
    }

    private void DistributeLockedProjectDay(EditableTimesheetDay day)
    {
        foreach (ProjectColumn project in _sheet.Projects)
        {
            if (!day.ProjectHoursFixed.GetValueOrDefault(project.Id))
            {
                day.ProjectHours[project.Id] = day.ProjectFloor(project.Id);
            }
        }

        if (!day.CoreHoursFixed)
        {
            decimal projectHours = TimesheetLogic.Normalize(_sheet.Projects.Sum(project => day.ProjectHours.GetValueOrDefault(project.Id)));
            day.CoreHours = TimesheetLogic.Normalize(Math.Max(0m, AvailableDayCapacity(day) - projectHours));
        }
    }

    private decimal AvailableMonthCapacity() =>
        TimesheetLogic.Normalize(_sheet.Days.Sum(AvailableDayCapacity));

    private decimal AvailableDayCapacity(EditableTimesheetDay day) =>
        TimesheetInterruptions.HasBusinessTripInterruption(day.Description)
            ? 0m
            : TimesheetInterruptionHours.DayCapacity(day.Date, day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd, day.Description, _totalWorkload, tracksAttendance: true, day.Schedules);
}
