using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Notifications.Endpoints;

public class MarkAllNotificationsAsRead : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/read-all", Handle)
           .WithSummary("Mark All Notifications As Read");

    public sealed record Response(bool Success);

    private static async Task<Ok<Response>> Handle(AppDbContext dbContext, ICurrentUser currentUser, CancellationToken cancellationToken)
    {
        var notifications = await dbContext.Notifications
            .Where(x => x.EmployeeId == currentUser.EmployeeId && !x.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new Response(true));
    }
}
