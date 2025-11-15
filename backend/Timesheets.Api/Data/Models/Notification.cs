namespace Timesheets.Api.Data.Models;

public class Notification
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}
