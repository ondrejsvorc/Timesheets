using Microsoft.AspNetCore.Http.HttpResults;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Notifications.Endpoints;

public class MarkNotificationAsRead : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/{id}/read", Handle)
           .WithSummary("Mark Notification As Read");

    public sealed record Response(bool Success);

    private static async Task<Results<Ok<Response>, NotFound>> Handle(Guid id, AppDbContext dbContext, ICurrentUser currentUser, CancellationToken cancellationToken)
    {
        Notification? notification = await dbContext.Notifications.FindAsync([id], cancellationToken);
        if (notification is null || notification.EmployeeId != currentUser.EmployeeId)
        {
            return TypedResults.NotFound();
        }

        notification.IsRead = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new Response(true));
    }
}
