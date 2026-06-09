using System.Text.Json;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Timesheets;

internal static class AttendanceTimesheetReviewMapper
{
    public static AttendanceTimesheet ToReviewInput(Data.Models.AttendanceTimesheet timesheet)
    {
        List<AttendanceDay> attendanceDays = timesheet.Days.Select(MapDay).ToList();

        return new AttendanceTimesheet(
            EmployeePersonalNumber: timesheet.Employee.PersonalNumber,
            EmployeeName: timesheet.Employee.FullName,
            Workload: attendanceDays.FirstOrDefault()?.Workload ?? 0m,
            Year: timesheet.Year,
            Month: timesheet.Month,
            Days: attendanceDays);
    }

    public static TimesheetReview Review(Data.Models.AttendanceTimesheet timesheet)
    {
        AttendanceTimesheetReviewer reviewer = new();
        return reviewer.Review(ToReviewInput(timesheet));
    }

    private static AttendanceDay MapDay(Data.Models.AttendanceDay day)
    {
        List<TimeRange> schedules = string.IsNullOrWhiteSpace(day.Schedules)
            ? []
            : JsonSerializer.Deserialize<List<TimeRange>>(day.Schedules) ?? [];

        return new AttendanceDay(
            Date: day.Date,
            ClockIn: day.ClockIn,
            ClockOut: day.ClockOut,
            BreakStart: day.BreakStart,
            BreakEnd: day.BreakEnd,
            OtherInterruption: null,
            Schedules: schedules,
            IsHoliday: day.IsHoliday,
            Workload: day.Workload);
    }
}
