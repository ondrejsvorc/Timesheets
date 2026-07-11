using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Timesheets.Endpoints;

public sealed class DeleteTimesheetComment : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/combined/comments/{commentId}", Handle)
           .WithSummary("Delete Combined Timesheet Comment");

    public sealed record Request([FromQuery] Guid EmployeeId, [FromQuery] int Year, [FromQuery] int Month);

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> Handle(Guid commentId, [AsParameters] Request request, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!await user.CanAccessEmployeeAsync(request.EmployeeId, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        CombinedTimesheetScope? scope = await CombinedTimesheetScopeLoader.LoadAsync(request.EmployeeId, request.Year, request.Month, dbContext, cancellationToken);

        if (scope is null)
        {
            return TypedResults.NotFound();
        }

        Data.Models.TimesheetComment? comment = await dbContext.TimesheetComments.FirstOrDefaultAsync(entry => entry.Id == commentId, cancellationToken);

        if (comment is null)
        {
            return TypedResults.NotFound();
        }

        if (comment.AuthorEmployeeId != user.EmployeeId)
        {
            return TypedResults.Forbid();
        }

        IReadOnlyList<Guid> projectTimesheetIds = scope.ProjectTimesheetLabels.Keys.ToList();
        bool isAttendanceComment = comment.TimesheetId == scope.TimesheetId;
        bool isProjectComment = comment.ProjectTimesheetId is not null && projectTimesheetIds.Contains(comment.ProjectTimesheetId.Value);

        if (!isAttendanceComment && !isProjectComment)
        {
            return TypedResults.NotFound();
        }

        dbContext.TimesheetComments.Remove(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
