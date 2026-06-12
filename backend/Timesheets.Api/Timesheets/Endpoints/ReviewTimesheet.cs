using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class ReviewTimesheet : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/{id}/review", Handle)
           .WithSummary("Review Timesheet");

    public sealed record Response(TimesheetReview review);

    private static async Task<Results<Ok<Response>, NotFound>> Handle(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Data.Models.AttendanceTimesheet? timesheet = await dbContext.AttendanceTimesheets
            .Include(t => t.Days)
            .Include(t => t.Employee)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (timesheet is null)
        {
            return TypedResults.NotFound();
        }

        TimesheetReview review = await CombinedTimesheetReviewMapper.ReviewAsync(timesheet, dbContext, cancellationToken);

        return TypedResults.Ok(new Response(review));
    }
}
