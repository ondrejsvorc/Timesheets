using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class ReviewAttendanceTimesheet : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("/attendance/review", Handle)
        .WithSummary("Zkontroluje výkaz pracovní doby.");

    public sealed record Request(AttendanceTimesheet Timesheet);
    public sealed record Response(TimesheetReview Review);

    private static Ok<Response> Handle([FromBody] Request request, [FromServices] ITimesheetReviewer<AttendanceTimesheet> reviewer)
    {
        TimesheetReview review = reviewer.Review(request.Timesheet);
        Response response = new(review);
        return TypedResults.Ok(response);
    }
}