namespace Timesheets.Api.Domain.Models;

public sealed class Attendance
{
    public Guid Id { get; set; }
    public Guid TimesheetId { get; set; }
    public Guid EmployeeTypeId { get; set; }

    public Timesheet Timesheet { get; set; } = null!;
    public EmployeeType EmployeeType { get; set; } = null!;
    public ICollection<AttendanceDay> Days { get; set; } = [];
}
