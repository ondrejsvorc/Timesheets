using System.Globalization;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;
using Timesheets.Api.Notifications;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class UpdateCombinedTimesheetStatus : IEndpoint
{
    private static readonly Guid DraftStatusId = Guid.Parse("00000000-0000-0000-0000-000000000020");
    private static readonly Guid SubmittedStatusId = Guid.Parse("00000000-0000-0000-0000-000000000021");
    private static readonly Guid ApprovedStatusId = Guid.Parse("00000000-0000-0000-0000-000000000022");

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/combined/status", Handle)
           .WithSummary("Update Combined Timesheet Status")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request(
        Guid EmployeeId,
        int Year,
        int Month,
        Guid StatusId,
        string? Comment,
        IReadOnlyList<Guid> TimesheetIds);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.EmployeeId).NotEmpty();
            RuleFor(x => x.Year).GreaterThan(0);
            RuleFor(x => x.Month).InclusiveBetween(1, 12);
            RuleFor(x => x.StatusId).NotEmpty();
            RuleFor(x => x.TimesheetIds).NotEmpty();
            RuleFor(x => x.Comment).MaximumLength(500).When(x => x.Comment is not null);
        }
    }

    private static async Task<Results<Ok, BadRequest<string>, NotFound, UnauthorizedHttpResult>> Handle(
        [FromBody] Request request,
        HttpContext httpContext,
        AppDbContext dbContext,
        NotificationSender notificationSender,
        CancellationToken cancellationToken)
    {
        Employee changedBy = await CurrentEmployeeResolver.GetRequiredAsync(httpContext.User, dbContext, cancellationToken);

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

        Data.Models.AttendanceTimesheet? attendanceTimesheet = await dbContext.AttendanceTimesheets
            .Include(t => t.TimesheetStatus)
            .FirstOrDefaultAsync(t => t.Id == scope.AttendanceTimesheetId, cancellationToken);

        if (attendanceTimesheet is null)
        {
            return TypedResults.NotFound();
        }

        TimesheetStatus? newStatus = await dbContext.TimesheetStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.StatusId, cancellationToken);

        if (newStatus is null)
        {
            return TypedResults.BadRequest($"Status with ID '{request.StatusId}' not found in database.");
        }

        Guid currentStatusId = attendanceTimesheet.TimesheetStatusId;
        string currentStatusName = attendanceTimesheet.TimesheetStatus.Name;

        if (!IsValidStatusTransition(currentStatusId, request.StatusId))
        {
            return TypedResults.BadRequest(
                $"Invalid status transition from '{currentStatusName}' (ID: {currentStatusId}) to '{newStatus.Name}' (ID: {request.StatusId}).");
        }

        HashSet<Guid> selectedIds = request.TimesheetIds.ToHashSet();
        HashSet<Guid> validIds = scope.ProjectTimesheetLabels.Keys
            .Append(scope.AttendanceTimesheetId)
            .ToHashSet();

        if (selectedIds.Any(id => !validIds.Contains(id)))
        {
            return TypedResults.BadRequest("One or more selected timesheets are invalid for this employee and period.");
        }

        bool statusChanged = currentStatusId != request.StatusId;
        if (statusChanged)
        {
            attendanceTimesheet.TimesheetStatusId = request.StatusId;
            attendanceTimesheet.UpdatedAt = DateTime.UtcNow;

            if (request.StatusId == SubmittedStatusId && attendanceTimesheet.SubmittedAt is null)
            {
                attendanceTimesheet.SubmittedAt = DateTime.UtcNow;
            }
            else if (request.StatusId == ApprovedStatusId)
            {
                attendanceTimesheet.ApprovedBy = changedBy.Id;
                attendanceTimesheet.ApprovedAt = DateTime.UtcNow;
            }
        }

        List<Guid> historyTargetIds = selectedIds.ToList();
        if (statusChanged && !historyTargetIds.Contains(scope.AttendanceTimesheetId))
        {
            historyTargetIds.Add(scope.AttendanceTimesheetId);
        }

        foreach (Guid timesheetId in historyTargetIds.Distinct())
        {
            bool isAttendance = timesheetId == scope.AttendanceTimesheetId;
            if (!isAttendance && !scope.ProjectTimesheetLabels.ContainsKey(timesheetId))
            {
                continue;
            }

            if (!statusChanged && string.IsNullOrWhiteSpace(request.Comment))
            {
                continue;
            }

            TimesheetStatusHistory history = new()
            {
                Id = Guid.NewGuid(),
                AttendanceTimesheetId = isAttendance ? timesheetId : null,
                ProjectTimesheetId = isAttendance ? null : timesheetId,
                FromStatusId = statusChanged ? currentStatusId : request.StatusId,
                ToStatusId = request.StatusId,
                ChangedByEmployeeId = changedBy.Id,
                Comment = request.Comment,
            };

            dbContext.TimesheetStatusHistories.Add(history);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (statusChanged)
        {
            string notificationMessage = BuildNotificationMessage(
                request.Year,
                request.Month,
                currentStatusName,
                newStatus.Name,
                request.Comment);

            await notificationSender.SendAsync(attendanceTimesheet.EmployeeId, notificationMessage, cancellationToken);
        }

        return TypedResults.Ok();
    }

    private static bool IsValidStatusTransition(Guid from, Guid to) => (from, to) switch
    {
        (Guid f, Guid t) when f == DraftStatusId && t == SubmittedStatusId => true,
        (Guid f, Guid t) when f == SubmittedStatusId && t == ApprovedStatusId => true,
        (Guid f, Guid t) when f == SubmittedStatusId && t == DraftStatusId => true,
        (Guid f, Guid t) when f == ApprovedStatusId && t == DraftStatusId => true,
        (Guid f, Guid t) when f == t => true,
        _ => false
    };

    private static string BuildNotificationMessage(int year, int month, string oldStatus, string newStatus, string? comment)
    {
        string monthName = new DateTime(year, month, 1).ToString("MMMM yyyy", CultureInfo.GetCultureInfo("cs-CZ"));
        string message = $"Stav vašeho výkazu za {monthName} byl změněn z '{oldStatus}' na '{newStatus}'.";

        if (!string.IsNullOrWhiteSpace(comment))
        {
            message += $"\n\nKomentář: {comment}";
        }

        return message;
    }
}
