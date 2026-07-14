namespace Timesheets.Api.Domain.Models;

public sealed class EmployeeType
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Employee> Employees { get; set; } = [];
}
