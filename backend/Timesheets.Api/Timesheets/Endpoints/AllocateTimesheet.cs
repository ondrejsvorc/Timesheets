using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Timesheets.Api.Auth;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class AllocateTimesheet : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/{id}/allocate", Handle)
            .WithSummary("Allocate Timesheet Draft")
            .WithRequestValidation<TimesheetDraft>();

    private static async Task<Results<Ok<TimesheetAllocation>, NotFound, ForbidHttpResult>> Handle(
        Guid id,
        [FromQuery] int? day,
        [FromBody] TimesheetDraft draft,
        AppDbContext dbContext,
        ICurrentUser user,
        CancellationToken cancellationToken)
    {
        TimesheetDraftContext? context = await TimesheetDrafts.LoadAsync(id, dbContext, cancellationToken);
        if (context is null)
        {
            return TypedResults.NotFound();
        }
        if (user.EmployeeId != context.Timesheet.EmployeeId || context.Timesheet.TimesheetStatusId != TimesheetWorkflowConstants.DraftStatusId)
        {
            return TypedResults.Forbid();
        }

        return TypedResults.Ok(TimesheetAllocator.Allocate(context, draft, day));
    }
}
