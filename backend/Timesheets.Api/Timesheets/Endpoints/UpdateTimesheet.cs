using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Timesheets.Api.Auth;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class UpdateTimesheet : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/{id}", Handle)
            .WithSummary("Update Timesheet")
            .WithRequestValidation<TimesheetDraft>();

    public sealed record Response(Guid Id, TimesheetEvaluation Evaluation);

    private static async Task<Results<Ok<Response>, NotFound, ForbidHttpResult>> Handle(Guid id, [FromBody] TimesheetDraft draft, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        TimesheetDraftContext? context = await TimesheetDrafts.LoadAsync(id, dbContext, cancellationToken);
        if (context is null)
        {
            return TypedResults.NotFound();
        }
        if ((!user.IsGlobalManagerRole() && user.EmployeeId != context.Timesheet.EmployeeId) || context.Timesheet.TimesheetStatusId != TimesheetWorkflow.DraftStatusId)
        {
            return TypedResults.Forbid();
        }
        TimesheetDrafts.Apply(context, draft);
        TimesheetEvaluation evaluation = TimesheetDrafts.Evaluate(context, draft);
        await dbContext.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(new Response(id, evaluation));
    }
}
