namespace Timesheets.Api.Domain.Models;

public sealed class ContractManager
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public Guid EmployeeId { get; set; }

    public Contract Contract { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}
