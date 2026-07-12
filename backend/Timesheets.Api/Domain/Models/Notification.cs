namespace Timesheets.Api.Domain.Models;

public sealed class Notification
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;

    public Employee Employee { get; set; } = null!;
}
