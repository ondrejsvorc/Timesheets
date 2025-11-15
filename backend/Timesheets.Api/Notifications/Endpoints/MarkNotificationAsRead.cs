using Microsoft.AspNetCore.Http.HttpResults;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Notifications.Endpoints;

public class MarkNotificationAsRead : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/{id:guid}/read", Handle)
           .WithSummary("Mark Notification As Read");

    public sealed record Response(bool Success);

    private static async Task<Results<Ok<Response>, NotFound>> Handle(Guid id, AppDbContext db, CancellationToken cancellationToken)
    {
        Notification? notification = await db.Notifications.FindAsync([id], cancellationToken);
        if (notification is null)
        {
            return TypedResults.NotFound();
        }

        notification.IsRead = true;
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new Response(true));
    }
}
