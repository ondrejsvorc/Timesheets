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
            .WithRequestValidation<TimesheetEditRequest>();

    public sealed record Response(Guid Id, TimesheetEvaluation Evaluation);

    private static async Task<Results<Ok<Response>, NotFound, BadRequest<string>, ForbidHttpResult>> Handle(Guid id, [FromBody] TimesheetEditRequest request, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        LoadedTimesheet? loaded = await TimesheetEngine.LoadAsync(id, dbContext, cancellationToken);
        if (loaded is null)
        {
            return TypedResults.NotFound();
        }
        if ((!user.IsGlobalManagerRole() && user.EmployeeId != loaded.Timesheet.EmployeeId) || loaded.Timesheet.TimesheetStatusId != TimesheetWorkflow.DraftStatusId)
        {
            return TypedResults.Forbid();
        }
        if (TimesheetEngine.HasInactiveProjectHours(loaded, request))
        {
            return TypedResults.BadRequest("Zakázkové hodiny nelze vyplnit mimo platnost pozice nebo projektu.");
        }
        TimesheetEngine.ApplyEdits(loaded, request);
        TimesheetEvaluation evaluation = TimesheetEngine.Evaluate(loaded, request);
        await dbContext.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(new Response(id, evaluation));
    }
}
