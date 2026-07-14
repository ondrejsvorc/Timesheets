namespace Timesheets.Api.Domain.Models;

public sealed class TimesheetComment
{
    public Guid Id { get; set; }

    public Guid? TimesheetId { get; set; }
    public Guid? ContractPartId { get; set; }

    public Guid AuthorEmployeeId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public Timesheet? Timesheet { get; set; }
    public ContractPart? ContractPart { get; set; }
    public Employee AuthorEmployee { get; set; } = null!;
}
