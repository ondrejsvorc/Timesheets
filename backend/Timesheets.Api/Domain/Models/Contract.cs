namespace Timesheets.Api.Domain.Models;

public sealed class Contract
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string NormalizedRegistrationNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Project Project { get; set; } = null!;
    public ICollection<ContractManager> ContractManagers { get; set; } = [];
    public ICollection<ContractEmployee> ContractEmployees { get; set; } = [];
}
