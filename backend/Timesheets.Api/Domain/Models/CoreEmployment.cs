namespace Timesheets.Api.Domain.Models;

public sealed class CoreEmployment
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public decimal Workload { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public Employee Employee { get; set; } = null!;
}
