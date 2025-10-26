namespace Timesheets.Api.Timesheets;

public interface ITimesheetReader<T>
{
    T Read(Stream stream);
}

public sealed class AttendanceTimesheetReader : ITimesheetReader<AttendanceTimesheet>
{
    public AttendanceTimesheet Read(Stream stream)
    {
        throw new NotImplementedException();
    }
}

public sealed class ProjectTimesheetReader : ITimesheetReader<ProjectTimesheet>
{
    public ProjectTimesheet Read(Stream stream)
    {
        throw new NotImplementedException();
    }
}