namespace Timesheets.Api.Domain.Models;

public sealed class ContractPart
{
    public Guid Id { get; set; }
    public Guid TimesheetId { get; set; }
    public Guid ContractEmployeeId { get; set; }
    public Guid TimesheetStatusId { get; set; }
    public decimal Workload { get; set; }
    public DateTime? LockedAt { get; set; }
    public Guid? LockedBy { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public TimesheetStatus TimesheetStatus { get; set; } = null!;
    public Timesheet Timesheet { get; set; } = null!;
    public ContractEmployee ContractEmployee { get; set; } = null!;
    public ICollection<ContractPartDay> Days { get; set; } = [];
    public ICollection<TimesheetStatusHistory> StatusHistory { get; set; } = [];
    public ICollection<TimesheetComment> Comments { get; set; } = [];
}
