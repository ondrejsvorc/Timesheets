namespace Timesheets.Api.Domain.Models;

public sealed class EmployeeWorkload
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Workload { get; set; }

    public Employee Employee { get; set; } = null!;
}
