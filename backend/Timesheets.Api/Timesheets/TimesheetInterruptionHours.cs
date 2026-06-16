namespace Timesheets.Api.Timesheets;

internal static class TimesheetInterruptionHours
{
    public static decimal DayCapacity(DateTime date, TimeSpan? clockIn, TimeSpan? clockOut, TimeSpan? breakStart, TimeSpan? breakEnd, string? description, decimal totalWorkload, bool tracksAttendance, IReadOnlyList<TimeRange>? schedules = null)
    {
        if (tracksAttendance)
        {
            if (clockIn is not null || clockOut is not null || breakStart is not null || breakEnd is not null)
            {
                decimal worked = TimesheetLogic.CalculateWorkedHoursFromAttendance(clockIn, clockOut, breakStart, breakEnd);
                if (worked > 0m)
                {
                    return Math.Min(12m, worked);
                }
            }

            return string.IsNullOrWhiteSpace(description) ? 0m : TimesheetLogic.Normalize(8m * totalWorkload);
        }

        if (TimesheetLogic.IsWeekday(date) || !string.IsNullOrWhiteSpace(description))
        {
            return TimesheetLogic.Normalize(8m * totalWorkload);
        }

        decimal stagHours = schedules is { Count: > 0 } ? TimesheetLogic.CalculateStagHours(schedules) : 0m;
        return stagHours > 0m ? TimesheetLogic.Normalize(Math.Min(12m, stagHours)) : 0m;
    }

    public static void ApplyToDayState(TimesheetDraftDayState day, IReadOnlyList<TimesheetDraftProjectState> projects, decimal totalWorkload, bool tracksAttendance)
    {
        if (TimesheetInterruptions.HasBusinessTripInterruption(day.Description))
        {
            return;
        }

        decimal capacity = DayCapacity(day.Date, day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd, day.Description, totalWorkload, tracksAttendance, day.Schedules);
        if (capacity <= 0m)
        {
            return;
        }

        if (TimesheetInterruptions.HasProportionalInterruption(day.Description))
        {
            ApplyProportional(day, projects, totalWorkload, capacity);
        }
    }

    private static void ApplyProportional(TimesheetDraftDayState day, IReadOnlyList<TimesheetDraftProjectState> projects, decimal totalWorkload, decimal capacity)
    {
        if (totalWorkload <= 0m)
        {
            return;
        }

        decimal projectWorkload = projects.Sum(project => project.Workload);
        decimal coreWorkload = Math.Max(0m, totalWorkload - projectWorkload);
        decimal allocated = 0m;
        day.CoreHours = TimesheetLogic.Normalize(capacity * coreWorkload / totalWorkload);
        allocated += day.CoreHours;

        for (int index = 0; index < projects.Count; index++)
        {
            TimesheetDraftProjectState project = projects[index];
            decimal hours = index == projects.Count - 1 ? TimesheetLogic.Normalize(capacity - allocated) : TimesheetLogic.Normalize(capacity * project.Workload / totalWorkload);
            day.ProjectHours[project.Id] = hours;
            allocated += hours;
        }
    }
}
