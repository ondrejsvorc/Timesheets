using Microsoft.AspNetCore.SignalR;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Notifications;

public class NotificationSender(AppDbContext db, IHubContext<NotificationHub> hub)
{
    public async Task SendAsync(Guid employeeId, string message, CancellationToken cancellationToken)
    {
        Notification notification = new()
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Message = message,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync(cancellationToken);

        await hub.Clients.User(employeeId.ToString())
            .SendAsync(method: "notification", new
            {
                notification.Id,
                notification.Message,
                notification.CreatedAt
            }, cancellationToken);
    }
}