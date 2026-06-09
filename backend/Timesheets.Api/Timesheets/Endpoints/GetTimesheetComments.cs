using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
using Timesheets.Api.Auth;
using Timesheets.Api.Common;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class GetTimesheetComments : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/combined/comments", Handle)
           .WithSummary("Get Combined Timesheet Comments");

    public sealed record Request([FromQuery] Guid EmployeeId, [FromQuery] int Year, [FromQuery] int Month);
    public sealed record CommentAuthor(string Name, string Role);
    public sealed record StatusChangeDetails(
        CommentAuthor ChangedBy,
        string TimesheetLabel,
        string? FromStatus,
        string ToStatus,
        string? Comment);
    public sealed record CommentItem(
        Guid Id,
        string Type,
        DateTime CreatedAt,
        string? Text,
        CommentAuthor? Author,
        StatusChangeDetails? StatusChange);

    private static async Task<Results<Ok<IReadOnlyList<CommentItem>>, NotFound, ForbidHttpResult>> Handle(
        [AsParameters] Request request,
        HttpContext httpContext,
        AppDbContext dbContext,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        (_, UserPermissionsScope permissionsScope) = await PermissionsScopeResolver.ResolveRequiredAsync(
            httpContext, dbContext, administrationOptions, cancellationToken);

        if (!await ApiPermissions.CanAccessEmployeeAsync(permissionsScope, request.EmployeeId, dbContext, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        CombinedTimesheetScope? scope = await CombinedTimesheetScopeLoader.LoadAsync(
            request.EmployeeId,
            request.Year,
            request.Month,
            dbContext,
            cancellationToken);

        if (scope is null)
        {
            return TypedResults.NotFound();
        }

        IReadOnlyList<Guid> projectIds = scope.ProjectTimesheetLabels.Keys.ToList();

        List<Data.Models.TimesheetComment> comments = await dbContext.TimesheetComments
            .AsNoTracking()
            .Include(c => c.AuthorEmployee)
            .Where(c =>
                c.AttendanceTimesheetId == scope.AttendanceTimesheetId
                || (c.ProjectTimesheetId != null && projectIds.Contains(c.ProjectTimesheetId.Value)))
            .ToListAsync(cancellationToken);

        List<TimesheetStatusHistory> history = await dbContext.TimesheetStatusHistories
            .AsNoTracking()
            .Include(h => h.FromStatus)
            .Include(h => h.ToStatus)
            .Include(h => h.ChangedByEmployee)
            .Where(h =>
                h.AttendanceTimesheetId == scope.AttendanceTimesheetId
                || (h.ProjectTimesheetId != null && projectIds.Contains(h.ProjectTimesheetId.Value)))
            .ToListAsync(cancellationToken);

        List<CommentItem> items = comments
            .Select(comment => new CommentItem(
                comment.Id,
                "message",
                comment.CreatedAt,
                comment.Text,
                new CommentAuthor(
                    EmployeeNameFormatter.Format(
                        comment.AuthorEmployee.TitleBefore,
                        comment.AuthorEmployee.FullName,
                        comment.AuthorEmployee.TitleAfter),
                    EmployeeRoleFormatter.FormatApiRole(comment.AuthorEmployee)),
                null))
            .Concat(history.Select(entry => new CommentItem(
                entry.Id,
                "statusChange",
                entry.ChangedAt,
                null,
                null,
                new StatusChangeDetails(
                    new CommentAuthor(
                        EmployeeNameFormatter.Format(
                            entry.ChangedByEmployee.TitleBefore,
                            entry.ChangedByEmployee.FullName,
                            entry.ChangedByEmployee.TitleAfter),
                        EmployeeRoleFormatter.FormatApiRole(entry.ChangedByEmployee)),
                    scope.ResolveTimesheetLabel(entry.AttendanceTimesheetId, entry.ProjectTimesheetId),
                    entry.FromStatus?.Name,
                    entry.ToStatus.Name,
                    entry.Comment))))
            .OrderBy(item => item.CreatedAt)
            .ToList();

        return TypedResults.Ok<IReadOnlyList<CommentItem>>(items);
    }
}
