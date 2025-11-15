using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Notifications.Endpoints;

public class GetEmployeeNotifications : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/", Handle)
           .WithSummary("Get Employee Notifications");

    public sealed record Response(Guid Id, string Message, DateTime CreatedAt, bool IsRead);

    private static async Task<Ok<List<Response>>> Handle(AppDbContext db, Guid employeeId, CancellationToken cancellationToken)
    {
        List<Response> notifications = await db.Notifications
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new Response(x.Id, x.Message, x.CreatedAt, x.IsRead))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(notifications);
    }
}
