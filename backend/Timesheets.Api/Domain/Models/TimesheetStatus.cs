namespace Timesheets.Api.Domain.Models;

public sealed class TimesheetStatus
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<Timesheet> Timesheets { get; set; } = [];
}
