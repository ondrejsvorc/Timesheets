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

        HashSet<Guid> selectedIds = request.TimesheetIds.ToHashSet();
        bool includesAttendance = selectedIds.Contains(scope.AttendanceTimesheetId);
        HashSet<Guid> selectedProjectIds = selectedIds
            .Where(id => scope.ProjectTimesheetLabels.ContainsKey(id))
            .ToHashSet();

        if (includesAttendance && selectedProjectIds.Count > 0)
        {
            return TypedResults.BadRequest("Nelze měnit stav docházky a projektových výkazů v jednom požadavku.");
        }

        if (!includesAttendance && selectedProjectIds.Count == 0)
        {
            return TypedResults.BadRequest("One or more selected timesheets are invalid for this employee and period.");
        }

        if (selectedIds.Any(id => id != scope.AttendanceTimesheetId && !scope.ProjectTimesheetLabels.ContainsKey(id)))
        {
            return TypedResults.BadRequest("One or more selected timesheets are invalid for this employee and period.");
        }

        if (includesAttendance)
        {
            return await UpdateAttendanceStatusAsync(
                request,
                scope,
                attendanceTimesheet,
                newStatus,
                changedBy,
                dbContext,
                notificationSender,
                cancellationToken);
        }

        return await UpdateProjectStatusesAsync(
            request,
            scope,
            attendanceTimesheet,
            selectedProjectIds,
            newStatus,
            changedBy,
            dbContext,
            notificationSender,
            cancellationToken);
    }

    private static async Task<Results<Ok, BadRequest<string>, NotFound, UnauthorizedHttpResult>> UpdateAttendanceStatusAsync(
        Request request,
        CombinedTimesheetScope scope,
        Data.Models.AttendanceTimesheet attendanceTimesheet,
        TimesheetStatus newStatus,
        Employee changedBy,
        AppDbContext dbContext,
        NotificationSender notificationSender,
        CancellationToken cancellationToken)
    {
        Guid currentStatusId = attendanceTimesheet.TimesheetStatusId;
        string currentStatusName = attendanceTimesheet.TimesheetStatus.Name;

        if (!TimesheetWorkflowConstants.IsValidAttendanceTransition(currentStatusId, request.StatusId))
        {
            return TypedResults.BadRequest(
                $"Invalid status transition from '{currentStatusName}' (ID: {currentStatusId}) to '{newStatus.Name}' (ID: {request.StatusId}).");
        }

        if (request.StatusId == TimesheetWorkflowConstants.ApprovedStatusId)
        {
            bool allProjectsApproved = await AreAllProjectsApprovedAsync(scope, dbContext, cancellationToken);
            if (!allProjectsApproved)
            {
                return TypedResults.BadRequest("Před finálním schválením musí být schváleny všechny projektové části výkazu.");
            }
        }

        bool statusChanged = currentStatusId != request.StatusId;
        if (statusChanged)
        {
            attendanceTimesheet.TimesheetStatusId = request.StatusId;
            attendanceTimesheet.UpdatedAt = DateTime.UtcNow;

            if (request.StatusId == TimesheetWorkflowConstants.SubmittedStatusId && attendanceTimesheet.SubmittedAt is null)
            {
                attendanceTimesheet.SubmittedAt = DateTime.UtcNow;
            }
            else if (request.StatusId == TimesheetWorkflowConstants.ApprovedStatusId)
            {
                attendanceTimesheet.ApprovedBy = changedBy.Id;
                attendanceTimesheet.ApprovedAt = DateTime.UtcNow;
            }
            else if (request.StatusId == TimesheetWorkflowConstants.DraftStatusId)
            {
                attendanceTimesheet.ApprovedBy = null;
                attendanceTimesheet.ApprovedAt = null;
                await ResetAllProjectsToDraftAsync(scope, dbContext, cancellationToken);
            }
        }

        if (statusChanged || !string.IsNullOrWhiteSpace(request.Comment))
        {
            TimesheetStatusHistory history = new()
            {
                Id = Guid.NewGuid(),
                AttendanceTimesheetId = attendanceTimesheet.Id,
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

    private static async Task<Results<Ok, BadRequest<string>, NotFound, UnauthorizedHttpResult>> UpdateProjectStatusesAsync(
        Request request,
        CombinedTimesheetScope scope,
        Data.Models.AttendanceTimesheet attendanceTimesheet,
        HashSet<Guid> selectedProjectIds,
        TimesheetStatus newStatus,
        Employee changedBy,
        AppDbContext dbContext,
        NotificationSender notificationSender,
        CancellationToken cancellationToken)
    {
        if (request.StatusId == TimesheetWorkflowConstants.ApprovedStatusId
            && attendanceTimesheet.TimesheetStatusId != TimesheetWorkflowConstants.SubmittedStatusId)
        {
            return TypedResults.BadRequest("Projektovou část lze schválit až po odeslání celého výkazu ke schválení.");
        }

        if (request.StatusId == TimesheetWorkflowConstants.DraftStatusId
            && attendanceTimesheet.TimesheetStatusId == TimesheetWorkflowConstants.ApprovedStatusId)
        {
            return TypedResults.BadRequest("Schválený výkaz nelze částečně vrátit. Použijte odemčení celého výkazu.");
        }

        List<Data.Models.ProjectTimesheet> projectTimesheets = await dbContext.ProjectTimesheets
            .Include(t => t.TimesheetStatus)
            .Where(t => selectedProjectIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        if (projectTimesheets.Count != selectedProjectIds.Count)
        {
            return TypedResults.NotFound();
        }

        bool anyProjectStatusChanged = false;
        Guid attendanceStatusBefore = attendanceTimesheet.TimesheetStatusId;
        string attendanceStatusNameBefore = attendanceTimesheet.TimesheetStatus.Name;

        foreach (Data.Models.ProjectTimesheet projectTimesheet in projectTimesheets)
        {
            Guid currentStatusId = projectTimesheet.TimesheetStatusId;
            string currentStatusName = projectTimesheet.TimesheetStatus.Name;

            if (!TimesheetWorkflowConstants.IsValidProjectTransition(currentStatusId, request.StatusId))
            {
                return TypedResults.BadRequest(
                    $"Invalid status transition from '{currentStatusName}' (ID: {currentStatusId}) to '{newStatus.Name}' (ID: {request.StatusId}).");
            }

            bool statusChanged = currentStatusId != request.StatusId;
            if (!statusChanged && string.IsNullOrWhiteSpace(request.Comment))
            {
                continue;
            }

            if (statusChanged)
            {
                projectTimesheet.TimesheetStatusId = request.StatusId;
                projectTimesheet.UpdatedAt = DateTime.UtcNow;
                anyProjectStatusChanged = true;

                if (request.StatusId == TimesheetWorkflowConstants.ApprovedStatusId)
                {
                    projectTimesheet.LockedAt = DateTime.UtcNow;
                    projectTimesheet.LockedBy = changedBy.Id;
                }
                else if (request.StatusId == TimesheetWorkflowConstants.DraftStatusId)
                {
                    projectTimesheet.LockedAt = null;
                    projectTimesheet.LockedBy = null;
                }
            }

            TimesheetStatusHistory history = new()
            {
                Id = Guid.NewGuid(),
                ProjectTimesheetId = projectTimesheet.Id,
                FromStatusId = statusChanged ? currentStatusId : request.StatusId,
                ToStatusId = request.StatusId,
                ChangedByEmployeeId = changedBy.Id,
                Comment = request.Comment,
            };

            dbContext.TimesheetStatusHistories.Add(history);
        }

        bool attendanceReopened = false;
        if (anyProjectStatusChanged
            && request.StatusId == TimesheetWorkflowConstants.DraftStatusId
            && attendanceTimesheet.TimesheetStatusId == TimesheetWorkflowConstants.SubmittedStatusId)
        {
            attendanceTimesheet.TimesheetStatusId = TimesheetWorkflowConstants.DraftStatusId;
            attendanceTimesheet.UpdatedAt = DateTime.UtcNow;
            attendanceReopened = true;

            TimesheetStatusHistory attendanceHistory = new()
            {
                Id = Guid.NewGuid(),
                AttendanceTimesheetId = attendanceTimesheet.Id,
                FromStatusId = attendanceStatusBefore,
                ToStatusId = TimesheetWorkflowConstants.DraftStatusId,
                ChangedByEmployeeId = changedBy.Id,
                Comment = request.Comment,
            };

            dbContext.TimesheetStatusHistories.Add(attendanceHistory);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (anyProjectStatusChanged || attendanceReopened)
        {
            string notificationMessage = attendanceReopened
                ? BuildNotificationMessage(
                    request.Year,
                    request.Month,
                    attendanceStatusNameBefore,
                    TimesheetWorkflowConstants.DraftStatusName,
                    request.Comment)
                : BuildProjectNotificationMessage(request.Year, request.Month, newStatus.Name, request.Comment);

            await notificationSender.SendAsync(attendanceTimesheet.EmployeeId, notificationMessage, cancellationToken);
        }

        return TypedResults.Ok();
    }

    private static async Task<bool> AreAllProjectsApprovedAsync(
        CombinedTimesheetScope scope,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (scope.ProjectTimesheetLabels.Count == 0)
        {
            return true;
        }

        List<Guid> projectStatusIds = await dbContext.ProjectTimesheets
            .AsNoTracking()
            .Where(t => scope.ProjectTimesheetLabels.Keys.Contains(t.Id))
            .Select(t => t.TimesheetStatusId)
            .ToListAsync(cancellationToken);

        return projectStatusIds.Count == scope.ProjectTimesheetLabels.Count
            && projectStatusIds.All(id => id == TimesheetWorkflowConstants.ApprovedStatusId);
    }

    private static async Task ResetAllProjectsToDraftAsync(
        CombinedTimesheetScope scope,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (scope.ProjectTimesheetLabels.Count == 0)
        {
            return;
        }

        List<Data.Models.ProjectTimesheet> projectTimesheets = await dbContext.ProjectTimesheets
            .Where(t => scope.ProjectTimesheetLabels.Keys.Contains(t.Id))
            .ToListAsync(cancellationToken);

        foreach (Data.Models.ProjectTimesheet projectTimesheet in projectTimesheets)
        {
            projectTimesheet.TimesheetStatusId = TimesheetWorkflowConstants.DraftStatusId;
            projectTimesheet.LockedAt = null;
            projectTimesheet.LockedBy = null;
            projectTimesheet.UpdatedAt = DateTime.UtcNow;
        }
    }

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

    private static string BuildProjectNotificationMessage(int year, int month, string newStatus, string? comment)
    {
        string monthName = new DateTime(year, month, 1).ToString("MMMM yyyy", CultureInfo.GetCultureInfo("cs-CZ"));
        string message = $"Stav projektové části výkazu za {monthName} byl změněn na '{newStatus}'.";

        if (!string.IsNullOrWhiteSpace(comment))
        {
            message += $"\n\nKomentář: {comment}";
        }

        return message;
    }
}
