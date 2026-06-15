using System.Globalization;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Auth;
using Timesheets.Api.Common;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;
using Timesheets.Api.Notifications;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class UpdateCombinedTimesheetStatus : IEndpoint
{
    private const string SubmitAction = "submit";
    private const string ApproveAction = "approve";
    private const string ReturnAction = "return";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/combined/status", Handle)
           .WithSummary("Update Combined Timesheet Status")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request(Guid EmployeeId, int Year, int Month, string Action, string? Comment, IReadOnlyList<Guid> TimesheetIds);
    private sealed record TargetStatus(Guid Id, string Name);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.EmployeeId).NotEmpty();
            RuleFor(x => x.Year).GreaterThan(0);
            RuleFor(x => x.Month).InclusiveBetween(1, 12);
            RuleFor(x => x.Action).Must(IsSupportedAction);
            RuleFor(x => x.TimesheetIds).NotEmpty();
            RuleFor(x => x.Comment).MaximumLength(500).When(x => x.Comment is not null);
        }

        private static bool IsSupportedAction(string action) => action is SubmitAction or ApproveAction or ReturnAction;
    }

    private static async Task<Results<Ok, BadRequest<string>, NotFound, UnauthorizedHttpResult>> Handle([FromBody] Request request, AppDbContext dbContext, NotificationSender notificationSender, ICurrentUser user, CancellationToken cancellationToken)
    {
        CombinedTimesheetScope? scope = await CombinedTimesheetScopeLoader.LoadAsync(request.EmployeeId, request.Year, request.Month, dbContext, cancellationToken);

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

        TargetStatus targetStatus = ResolveTargetStatus(request.Action);

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
            return await UpdateAttendanceStatusAsync(request, scope, attendanceTimesheet, targetStatus, user, dbContext, notificationSender, cancellationToken);
        }

        return await UpdateProjectStatusesAsync(request, attendanceTimesheet, selectedProjectIds, targetStatus, user, dbContext, notificationSender, cancellationToken);
    }

    private static async Task<Results<Ok, BadRequest<string>, NotFound, UnauthorizedHttpResult>> UpdateAttendanceStatusAsync(Request request, CombinedTimesheetScope scope, Data.Models.AttendanceTimesheet attendanceTimesheet, TargetStatus targetStatus, ICurrentUser user, AppDbContext dbContext, NotificationSender notificationSender, CancellationToken cancellationToken)
    {
        Guid currentStatusId = attendanceTimesheet.TimesheetStatusId;
        string currentStatusName = attendanceTimesheet.TimesheetStatus.Name;

        if (!TimesheetWorkflow.IsValidAttendanceTransition(currentStatusId, targetStatus.Id))
        {
            return TypedResults.BadRequest($"Akci '{request.Action}' nelze provést ze stavu '{currentStatusName}'.");
        }

        if (user.EmployeeId != attendanceTimesheet.EmployeeId)
        {
            return TypedResults.Unauthorized();
        }

        bool statusWillChange = currentStatusId != targetStatus.Id;
        if (targetStatus.Id == TimesheetWorkflow.SubmittedStatusId && statusWillChange)
        {
            bool allProjectsApproved = await AreAllProjectsApprovedAsync(scope, dbContext, cancellationToken);
            if (!allProjectsApproved)
            {
                return TypedResults.BadRequest("Nejdříve musí manažeři schválit a uzamknout všechny projektové sloupce.");
            }

            TimesheetDraftContext? context = await TimesheetDrafts.LoadAsync(attendanceTimesheet.Id, dbContext, cancellationToken);
            if (context is null)
            {
                return TypedResults.NotFound();
            }

            TimesheetEvaluation evaluation = TimesheetDrafts.Evaluate(context, TimesheetDrafts.Current(context));
            if (evaluation.HasErrors)
            {
                return TypedResults.BadRequest("Výkaz obsahuje chyby a nelze ho odeslat ke schválení.");
            }
        }

        if (targetStatus.Id == TimesheetWorkflow.ApprovedStatusId)
        {
            bool allProjectsApproved = await AreAllProjectsApprovedAsync(scope, dbContext, cancellationToken);
            if (!allProjectsApproved)
            {
                return TypedResults.BadRequest("Před finálním schválením musí být schváleny všechny projektové části výkazu.");
            }
        }

        bool statusChanged = currentStatusId != targetStatus.Id;
        if (statusChanged)
        {
            attendanceTimesheet.TimesheetStatusId = targetStatus.Id;
            attendanceTimesheet.UpdatedAt = DateTime.UtcNow;

            if (targetStatus.Id == TimesheetWorkflow.SubmittedStatusId && attendanceTimesheet.SubmittedAt is null)
            {
                attendanceTimesheet.SubmittedAt = DateTime.UtcNow;
            }
            else if (targetStatus.Id == TimesheetWorkflow.ApprovedStatusId)
            {
                attendanceTimesheet.ApprovedBy = user.EmployeeId;
                attendanceTimesheet.ApprovedAt = DateTime.UtcNow;
            }
            else if (targetStatus.Id == TimesheetWorkflow.DraftStatusId)
            {
                attendanceTimesheet.ApprovedBy = null;
                attendanceTimesheet.ApprovedAt = null;
            }
        }

        if (statusChanged || !string.IsNullOrWhiteSpace(request.Comment))
        {
            TimesheetStatusHistory history = new()
            {
                Id = Guid.NewGuid(),
                AttendanceTimesheetId = attendanceTimesheet.Id,
                FromStatusId = statusChanged ? currentStatusId : targetStatus.Id,
                ToStatusId = targetStatus.Id,
                ChangedByEmployeeId = user.EmployeeId,
                Comment = request.Comment,
            };

            dbContext.TimesheetStatusHistories.Add(history);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (statusChanged)
        {
            string notificationMessage = BuildNotificationMessage(request.Year, request.Month, currentStatusName, targetStatus.Name, request.Comment);

            await notificationSender.SendAsync(attendanceTimesheet.EmployeeId, notificationMessage, cancellationToken);
        }

        return TypedResults.Ok();
    }

    private static async Task<Results<Ok, BadRequest<string>, NotFound, UnauthorizedHttpResult>> UpdateProjectStatusesAsync(Request request, Data.Models.AttendanceTimesheet attendanceTimesheet, HashSet<Guid> selectedProjectIds, TargetStatus targetStatus, ICurrentUser user, AppDbContext dbContext, NotificationSender notificationSender, CancellationToken cancellationToken)
    {
        List<ProjectTimesheetPart> projectScopes = await LoadProjectScopesAsync(selectedProjectIds, dbContext, cancellationToken);

        if (projectScopes.Count != selectedProjectIds.Count
            || !user.CanManageProjectTimesheetParts(projectScopes))
        {
            return TypedResults.Unauthorized();
        }

        if (attendanceTimesheet.TimesheetStatusId != TimesheetWorkflow.DraftStatusId)
        {
            return TypedResults.BadRequest("Projektové sloupce lze zamykat nebo odemykat pouze v rozpracovaném výkazu.");
        }

        List<Data.Models.ProjectTimesheet> projectTimesheets = await dbContext.ProjectTimesheets
            .Include(t => t.TimesheetStatus)
            .Where(t => selectedProjectIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        if (projectTimesheets.Count != selectedProjectIds.Count)
        {
            return TypedResults.NotFound();
        }

        bool isProjectReturn = targetStatus.Id == TimesheetWorkflow.DraftStatusId;
        bool anyProjectStatusChanged = false;

        foreach (Data.Models.ProjectTimesheet projectTimesheet in projectTimesheets)
        {
            Guid currentStatusId = projectTimesheet.TimesheetStatusId;
            string currentStatusName = projectTimesheet.TimesheetStatus.Name;

            if (!TimesheetWorkflow.IsValidProjectTransition(currentStatusId, targetStatus.Id))
            {
                return TypedResults.BadRequest($"Akci '{request.Action}' nelze provést ze stavu '{currentStatusName}'.");
            }

            bool statusChanged = currentStatusId != targetStatus.Id;

            if (!statusChanged && string.IsNullOrWhiteSpace(request.Comment))
            {
                continue;
            }

            if (statusChanged)
            {
                projectTimesheet.TimesheetStatusId = targetStatus.Id;
                anyProjectStatusChanged = true;
            }

            if (isProjectReturn)
            {
                projectTimesheet.LockedAt = null;
                projectTimesheet.LockedBy = null;
            }
            else if (targetStatus.Id == TimesheetWorkflow.ApprovedStatusId)
            {
                projectTimesheet.LockedAt = DateTime.UtcNow;
                projectTimesheet.LockedBy = user.EmployeeId;
            }

            if (statusChanged || !string.IsNullOrWhiteSpace(request.Comment))
            {
                projectTimesheet.UpdatedAt = DateTime.UtcNow;

                TimesheetStatusHistory history = new()
                {
                    Id = Guid.NewGuid(),
                    ProjectTimesheetId = projectTimesheet.Id,
                    FromStatusId = currentStatusId,
                    ToStatusId = targetStatus.Id,
                    ChangedByEmployeeId = user.EmployeeId,
                    Comment = request.Comment,
                };

                dbContext.TimesheetStatusHistories.Add(history);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (anyProjectStatusChanged)
        {
            string notificationMessage = BuildProjectNotificationMessage(request.Year, request.Month, targetStatus.Name, request.Comment);
            await notificationSender.SendAsync(attendanceTimesheet.EmployeeId, notificationMessage, cancellationToken);
        }

        return TypedResults.Ok();
    }

    private static TargetStatus ResolveTargetStatus(string action) => action switch
    {
        SubmitAction => new TargetStatus(TimesheetWorkflow.SubmittedStatusId, TimesheetWorkflow.SubmittedStatusName),
        ApproveAction => new TargetStatus(TimesheetWorkflow.ApprovedStatusId, TimesheetWorkflow.ApprovedStatusName),
        ReturnAction => new TargetStatus(TimesheetWorkflow.DraftStatusId, TimesheetWorkflow.DraftStatusName),
        _ => throw new InvalidOperationException($"Unsupported timesheet action '{action}'.")
    };

    private static async Task<bool> AreAllProjectsApprovedAsync(CombinedTimesheetScope scope, AppDbContext dbContext, CancellationToken cancellationToken)
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
            && projectStatusIds.All(id => id == TimesheetWorkflow.ApprovedStatusId);
    }

    private static Task<List<ProjectTimesheetPart>> LoadProjectScopesAsync(IEnumerable<Guid> projectTimesheetIds, AppDbContext dbContext, CancellationToken cancellationToken) => dbContext.ProjectTimesheets
        .AsNoTracking()
        .Where(timesheet => projectTimesheetIds.Contains(timesheet.Id))
        .Join(dbContext.ContractEmployees.AsNoTracking(), timesheet => timesheet.ContractEmployeeId, contractEmployee => contractEmployee.Id, (timesheet, contractEmployee) => new { contractEmployee.ContractId })
        .Join(dbContext.Contracts.AsNoTracking(), value => value.ContractId, contract => contract.Id, (value, contract) => new ProjectTimesheetPart(value.ContractId, contract.ProjectId))
        .ToListAsync(cancellationToken);

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
