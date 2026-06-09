using System.Globalization;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
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
        IOptions<AdministrationOptions> administrationOptions,
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

        EmployeeWorkflowPermissions workflowPermissions = await TimesheetWorkflowAuthorization.LoadAsync(
            changedBy,
            dbContext,
            administrationOptions,
            cancellationToken);

        if (includesAttendance)
        {
            return await UpdateAttendanceStatusAsync(
                request,
                scope,
                attendanceTimesheet,
                newStatus,
                workflowPermissions,
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
            workflowPermissions,
            dbContext,
            notificationSender,
            cancellationToken);
    }

    private static async Task<Results<Ok, BadRequest<string>, NotFound, UnauthorizedHttpResult>> UpdateAttendanceStatusAsync(
        Request request,
        CombinedTimesheetScope scope,
        Data.Models.AttendanceTimesheet attendanceTimesheet,
        TimesheetStatus newStatus,
        EmployeeWorkflowPermissions workflowPermissions,
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

        bool statusWillChange = currentStatusId != request.StatusId;
        if (statusWillChange)
        {
            bool isSubmit = request.StatusId == TimesheetWorkflowConstants.SubmittedStatusId;
            bool authorized = isSubmit
                ? TimesheetWorkflowAuthorization.CanSubmitTimesheet(workflowPermissions, attendanceTimesheet.EmployeeId)
                : TimesheetWorkflowAuthorization.CanManageWholeTimesheet(workflowPermissions);

            if (!authorized)
            {
                return TypedResults.Unauthorized();
            }
        }

        if (request.StatusId == TimesheetWorkflowConstants.SubmittedStatusId && statusWillChange)
        {
            await dbContext.Entry(attendanceTimesheet).Reference(t => t.Employee).LoadAsync(cancellationToken);
            await dbContext.Entry(attendanceTimesheet).Collection(t => t.Days).LoadAsync(cancellationToken);

            TimesheetReview review = AttendanceTimesheetReviewMapper.Review(attendanceTimesheet);
            if (review.HasErrors)
            {
                return TypedResults.BadRequest("Výkaz obsahuje chyby a nelze ho odeslat ke schválení.");
            }
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
                attendanceTimesheet.ApprovedBy = workflowPermissions.EmployeeId;
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
                ChangedByEmployeeId = workflowPermissions.EmployeeId,
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
        EmployeeWorkflowPermissions workflowPermissions,
        AppDbContext dbContext,
        NotificationSender notificationSender,
        CancellationToken cancellationToken)
    {
        List<ProjectTimesheetScope> projectScopes = await TimesheetWorkflowAuthorization.LoadProjectScopesAsync(
            selectedProjectIds,
            dbContext,
            cancellationToken);

        if (projectScopes.Count != selectedProjectIds.Count
            || !TimesheetWorkflowAuthorization.CanManageProjectTimesheets(workflowPermissions, projectScopes))
        {
            return TypedResults.Unauthorized();
        }

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

        bool isProjectReturn = request.StatusId == TimesheetWorkflowConstants.DraftStatusId;
        bool anyProjectStatusChanged = false;
        bool anyProjectReturnRecorded = false;
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
            bool isReturnWhilePendingReview = isProjectReturn
                && !statusChanged
                && attendanceTimesheet.TimesheetStatusId == TimesheetWorkflowConstants.SubmittedStatusId;

            if (!statusChanged && !isReturnWhilePendingReview && string.IsNullOrWhiteSpace(request.Comment))
            {
                continue;
            }

            if (statusChanged)
            {
                projectTimesheet.TimesheetStatusId = request.StatusId;
                anyProjectStatusChanged = true;
            }

            if (isProjectReturn)
            {
                projectTimesheet.LockedAt = null;
                projectTimesheet.LockedBy = null;
                anyProjectReturnRecorded = true;
            }
            else if (request.StatusId == TimesheetWorkflowConstants.ApprovedStatusId)
            {
                projectTimesheet.LockedAt = DateTime.UtcNow;
                projectTimesheet.LockedBy = workflowPermissions.EmployeeId;
            }

            if (statusChanged || isReturnWhilePendingReview || !string.IsNullOrWhiteSpace(request.Comment))
            {
                projectTimesheet.UpdatedAt = DateTime.UtcNow;

                TimesheetStatusHistory history = new()
                {
                    Id = Guid.NewGuid(),
                    ProjectTimesheetId = projectTimesheet.Id,
                    FromStatusId = statusChanged || isReturnWhilePendingReview
                        ? TimesheetWorkflowConstants.SubmittedStatusId
                        : currentStatusId,
                    ToStatusId = request.StatusId,
                    ChangedByEmployeeId = workflowPermissions.EmployeeId,
                    Comment = request.Comment,
                };

                dbContext.TimesheetStatusHistories.Add(history);
            }
        }

        bool attendanceReopened = false;
        if (isProjectReturn
            && anyProjectReturnRecorded
            && attendanceTimesheet.TimesheetStatusId == TimesheetWorkflowConstants.SubmittedStatusId)
        {
            attendanceTimesheet.TimesheetStatusId = TimesheetWorkflowConstants.DraftStatusId;
            attendanceTimesheet.UpdatedAt = DateTime.UtcNow;
            attendanceReopened = true;

            await ResetAllProjectsToDraftAsync(scope, dbContext, cancellationToken);

            TimesheetStatusHistory attendanceHistory = new()
            {
                Id = Guid.NewGuid(),
                AttendanceTimesheetId = attendanceTimesheet.Id,
                FromStatusId = attendanceStatusBefore,
                ToStatusId = TimesheetWorkflowConstants.DraftStatusId,
                ChangedByEmployeeId = workflowPermissions.EmployeeId,
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
