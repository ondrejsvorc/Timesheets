namespace Timesheets.Api.Domain.Models;

public sealed class ProjectManager
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid EmployeeId { get; set; }

    public Project Project { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}
