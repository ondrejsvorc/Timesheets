using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Timesheets.Api.Auth;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class ReviewTimesheet : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/{id}/review", Handle)
            .WithSummary("Review Timesheet Edit")
            .WithRequestValidation<TimesheetEditRequest>();

    private static async Task<Results<Ok<TimesheetEvaluation>, NotFound, ForbidHttpResult>> Handle(Guid id, [FromBody] TimesheetEditRequest request, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        LoadedTimesheet? loaded = await TimesheetEngine.LoadAsync(id, dbContext, cancellationToken);
        if (loaded is null)
        {
            return TypedResults.NotFound();
        }
        if (!await user.CanAccessEmployeeAsync(loaded.Timesheet.EmployeeId, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        return TypedResults.Ok(TimesheetEngine.Evaluate(loaded, request));
    }
}
