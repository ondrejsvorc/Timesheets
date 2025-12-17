using CzechHolidays;

namespace Timesheets.Api.Timesheets;

public interface ITimesheetImporter<T> where T : ITimesheet
{
    Task<T> ImportAsync(Stream stream);
}

public sealed class AttendanceTimesheetImporter(ITimesheetReader<AttendanceTimesheet> reader, ICzechHolidaysFactory factory) : ITimesheetImporter<AttendanceTimesheet>
{
    public async Task<AttendanceTimesheet> ImportAsync(Stream stream)
    {
        AttendanceTimesheet timesheet = reader.Read(stream);
        CzechHolidaysYear holidays = factory.Create(timesheet.Year);

        return timesheet with
        {
            Days = timesheet.Days
                .Select(day => day with { IsHoliday = holidays.Contains(day.Date) })
                .ToList()
        };
    }
}

[Obsolete]
public sealed class ProjectTimesheetImporter(ITimesheetReader<ProjectTimesheet> reader, ICzechHolidaysFactory factory) : ITimesheetImporter<ProjectTimesheet>
{
    public async Task<ProjectTimesheet> ImportAsync(Stream stream)
    {
        ProjectTimesheet timesheet = reader.Read(stream);
        CzechHolidaysYear holidays = factory.Create(timesheet.Year);

        return timesheet with
        {
            Days = timesheet.Days
                .Select(day => day with { IsHoliday = holidays.Contains(day.Date) })
                .ToList()
        };
    }
}
