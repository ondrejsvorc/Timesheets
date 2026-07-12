using Microsoft.AspNetCore.SignalR;
using Timesheets.Api.Domain;
using Timesheets.Api.Domain.Models;

namespace Timesheets.Api.Features.Notifications;

public class NotificationSender(AppDbContext db, IHubContext<NotificationHub> hub)
{
    public async Task SendAsync(Guid employeeId, string message, CancellationToken cancellationToken)
    {
        Notification notification = new()
        {
            Id = Guid.CreateVersion7(),
            EmployeeId = employeeId,
            Message = message
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
