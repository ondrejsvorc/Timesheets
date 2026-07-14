namespace Timesheets.Api.Domain.Models;

public sealed class Timesheet
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid TimesheetStatusId { get; set; }
    public Guid? ApprovedBy { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Employee Employee { get; set; } = null!;
    public TimesheetStatus TimesheetStatus { get; set; } = null!;
    public Employee ApprovedByEmployee { get; set; } = null!;
    public Attendance? Attendance { get; set; }
    public ICollection<ContractPart> ContractParts { get; set; } = [];
    public ICollection<TimesheetStatusHistory> StatusHistory { get; set; } = [];
    public ICollection<TimesheetComment> Comments { get; set; } = [];
}
