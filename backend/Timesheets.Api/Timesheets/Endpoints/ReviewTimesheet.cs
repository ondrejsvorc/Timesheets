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
            .WithSummary("Review Timesheet Draft")
            .WithRequestValidation<TimesheetDraft>();

    private static async Task<Results<Ok<TimesheetEvaluation>, NotFound, ForbidHttpResult>> Handle(Guid id, [FromBody] TimesheetDraft draft, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        TimesheetDraftContext? context = await TimesheetDrafts.LoadAsync(id, dbContext, cancellationToken);
        if (context is null)
        {
            return TypedResults.NotFound();
        }
        if (!await user.CanAccessEmployeeAsync(context.Timesheet.EmployeeId, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        return TypedResults.Ok(TimesheetDrafts.Evaluate(context, draft));
    }
}
