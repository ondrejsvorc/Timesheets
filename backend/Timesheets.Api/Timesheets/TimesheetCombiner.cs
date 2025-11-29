namespace Timesheets.Api.Timesheets;

interface ITimesheetCombiner
{
    public CombinedTimesheet Combine(AttendanceTimesheet attendance, IReadOnlyList<ProjectTimesheet> projects);
}

public class TimesheetCombiner : ITimesheetCombiner
{
    public CombinedTimesheet Combine(AttendanceTimesheet attendance, IReadOnlyList<ProjectTimesheet> projects)
    {
        IReadOnlyList<CombinedDay> days = CombineDays(attendance, projects);
        return new CombinedTimesheet(attendance.Year, attendance.Month, days);
    }

    private static IReadOnlyList<CombinedDay> CombineDays(AttendanceTimesheet attendance, IReadOnlyList<ProjectTimesheet> projects)
    {
        return attendance.Days
            .Select((attendanceDay, index) =>
            {
                List<ProjectDay> projectDays = projects
                    .Select(p => p.Days[index])
                    .ToList();

                return new CombinedDay(
                    Date: attendanceDay.Date,
                    IsHoliday: attendanceDay.IsHoliday,
                    IsWeekend: attendanceDay.IsWeekend,
                    IsWorkday: attendanceDay.IsWorkday,
                    AttendanceHours: attendanceDay.TotalHours,
                    ProjectHours: projectDays.Sum(d => d.Hours),
                    AttendanceWorkload: attendanceDay.Workload,
                    ProjectWorkload: projectDays.Sum(d => d.Workload)
                );
            })
            .ToList();
    }
}
