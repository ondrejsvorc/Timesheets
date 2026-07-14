namespace Timesheets.Api.Domain.Models;

public sealed class ContractEmployee
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public Guid EmployeeId { get; set; }
    public string PositionCode { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal Workload { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public Contract Contract { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}
