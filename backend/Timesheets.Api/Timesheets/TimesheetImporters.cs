namespace Timesheets.Api.Timesheets;

public interface ITimesheetImporter<T> where T : ITimesheet
{
    Task<T> ImportAsync(Stream stream);
}

public sealed class AttendanceTimesheetImporter(ITimesheetReader<AttendanceTimesheet> reader, IPublicHolidayProvider provider) : ITimesheetImporter<AttendanceTimesheet>
{
    public async Task<AttendanceTimesheet> ImportAsync(Stream stream)
    {
        AttendanceTimesheet timesheet = reader.Read(stream);

        IReadOnlyCollection<PublicHoliday> holidays = await provider.GetPublicHolidaysAsync(timesheet.Year);
        HashSet<DateTime> holidayDates = holidays.Select(h => h.Date).ToHashSet();

        return timesheet with
        {
            Days = timesheet.Days
                .Select(day => day with { IsHoliday = holidayDates.Contains(day.Date) })
                .ToList()
        };
    }
}

[Obsolete]
public sealed class ProjectTimesheetImporter(ITimesheetReader<ProjectTimesheet> reader, IPublicHolidayProvider provider) : ITimesheetImporter<ProjectTimesheet>
{
    public async Task<ProjectTimesheet> ImportAsync(Stream stream)
    {
        ProjectTimesheet timesheet = reader.Read(stream);

        IReadOnlyCollection<PublicHoliday> holidays = await provider.GetPublicHolidaysAsync(timesheet.Year);
        HashSet<DateTime> holidayDates = holidays.Select(h => h.Date).ToHashSet();

        return timesheet with
        {
            Days = timesheet.Days
                .Select(day => day with { IsHoliday = holidayDates.Contains(day.Date) })
                .ToList()
        };
    }
}
