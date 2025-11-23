using Microsoft.AspNetCore.Http.HttpResults;
using Timesheets.Api.Data;

namespace Timesheets.Api.Notifications.Endpoints;

public class MarkNotificationAsRead : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/{id}/read", Handle)
           .WithSummary("Mark Notification As Read");

    public sealed record Response(bool Success);

    private static async Task<Results<Ok<Response>, NotFound>> Handle(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Notification? notification = await dbContext.Notifications.FindAsync([id], cancellationToken);
        if (notification is null)
        {
            return TypedResults.NotFound();
        }

        notification.IsRead = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new Response(true));
    }
}
