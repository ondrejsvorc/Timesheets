namespace Timesheets.Api.Timesheets;

interface ITimesheetCombiner
{
    public CombinedTimesheet Combine(AttendanceTimesheet attendance, IReadOnlyList<ProjectTimesheet> projects, decimal coreWorkload);
}

public class TimesheetCombiner : ITimesheetCombiner
{
    public CombinedTimesheet Combine(AttendanceTimesheet attendance, IReadOnlyList<ProjectTimesheet> projects, decimal coreWorkload)
    {
        IReadOnlyList<CombinedDay> days = CombineDays(attendance, projects, coreWorkload);
        return new CombinedTimesheet(attendance.Year, attendance.Month, coreWorkload, days);
    }

    private static List<CombinedDay> CombineDays(AttendanceTimesheet attendance, IReadOnlyList<ProjectTimesheet> projects, decimal coreWorkload)
    {
        return attendance.Days.Select((attendanceDay, index) =>
        {
            List<ProjectDay> projectDays = projects
                .Select(p => p.Days[index])
                .ToList();

            return new CombinedDay(
                Date: attendanceDay.Date,
                IsHoliday: attendanceDay.IsHoliday,
                Workload: attendanceDay.Workload,
                CoreWorkload: coreWorkload,
                WorkedHours: attendanceDay.TotalHours,
                CoreHours: 0,
                ProjectHours: projectDays.Sum(d => d.Hours),
                StagHours: TimesheetLogic.CalculateStagHours(attendanceDay.Schedules),
                HasAttendanceFilled: attendanceDay.ClockIn is not null || attendanceDay.ClockOut is not null,
                SkipAllocationRules: TimesheetInterruptions.SkipAllocationRules(attendanceDay.OtherInterruption));
        })
        .ToList();
    }
}
