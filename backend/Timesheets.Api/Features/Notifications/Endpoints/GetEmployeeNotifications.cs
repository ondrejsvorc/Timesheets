using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Notifications.Endpoints;

public class GetEmployeeNotifications : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/", Handle)
           .WithSummary("Get Employee Notifications");

    public sealed record Response(Guid Id, string Message, DateTime CreatedAt, bool IsRead);

    private static async Task<Ok<List<Response>>> Handle(AppDbContext dbContext, ICurrentUser currentUser, CancellationToken cancellationToken)
    {
        List<Response> notifications = await dbContext.Notifications
            .Where(x => x.EmployeeId == currentUser.EmployeeId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .Select(x => new Response(x.Id, x.Message, x.CreatedAt, x.IsRead))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(notifications);
    }
}
