namespace Timesheets.Api.Domain.Models;

public sealed class TimesheetStatusHistory
{
    public Guid Id { get; set; }

    public Guid? TimesheetId { get; set; }
    public Guid? ContractPartId { get; set; }

    public Guid? FromStatusId { get; set; }
    public Guid ToStatusId { get; set; }

    public Guid ChangedByEmployeeId { get; set; }

    public DateTime ChangedAt { get; init; } = DateTime.UtcNow;
    public string? Comment { get; set; }

    public Timesheet? Timesheet { get; set; }
    public ContractPart? ContractPart { get; set; }

    public TimesheetStatus? FromStatus { get; set; }
    public TimesheetStatus ToStatus { get; set; } = null!;

    public Employee ChangedByEmployee { get; set; } = null!;
}
