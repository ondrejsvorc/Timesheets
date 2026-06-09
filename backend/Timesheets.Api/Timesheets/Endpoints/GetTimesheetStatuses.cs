using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class GetTimesheetStatuses : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/statuses", Handle)
           .WithSummary("Get Timesheet Statuses");

    public sealed record StatusItem(Guid Id, string Name);
    public sealed record Response(IEnumerable<StatusItem> Statuses);

    private static async Task<Ok<Response>> Handle(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        List<StatusItem> statuses = await dbContext.TimesheetStatuses
            .AsNoTracking()
            .OrderBy(status => status.Name)
            .Select(status => new StatusItem(status.Id, status.Name))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(statuses));
    }
}
