using System.Globalization;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;
using Timesheets.Api.Features.Auth;
using Timesheets.Api.Features.Notifications;

namespace Timesheets.Api.Features.Timesheets.Endpoints;

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
    private sealed record TargetStatus(Guid Id, string Code, string Name);

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

        Data.Models.Timesheet? monthTimesheet = await dbContext.Timesheets
            .Include(t => t.Employee)
            .Include(t => t.TimesheetStatus)
            .FirstOrDefaultAsync(t => t.Id == scope.TimesheetId, cancellationToken);

        if (monthTimesheet is null)
        {
            return TypedResults.NotFound();
        }

        TargetStatus targetStatus = ResolveTargetStatus(request.Action);

        HashSet<Guid> selectedIds = request.TimesheetIds.ToHashSet();
        bool includesAttendance = selectedIds.Contains(scope.TimesheetId);
        HashSet<Guid> selectedProjectIds = selectedIds
            .Where(id => scope.ContractPartLabels.ContainsKey(id))
            .ToHashSet();

        if (includesAttendance && selectedProjectIds.Count > 0)
        {
            return TypedResults.BadRequest("Nelze měnit stav docházky a projektových výkazů v jednom požadavku.");
        }

        if (!includesAttendance && selectedProjectIds.Count == 0)
        {
            return TypedResults.BadRequest("One or more selected timesheets are invalid for this employee and period.");
        }

        if (selectedIds.Any(id => id != scope.TimesheetId && !scope.ContractPartLabels.ContainsKey(id)))
        {
            return TypedResults.BadRequest("One or more selected timesheets are invalid for this employee and period.");
        }

        if (includesAttendance)
        {
            return await UpdateAttendanceStatusAsync(request, scope, monthTimesheet, targetStatus, user, dbContext, notificationSender, cancellationToken);
        }

        return await UpdateProjectStatusesAsync(request, monthTimesheet, selectedProjectIds, targetStatus, user, dbContext, notificationSender, cancellationToken);
    }

    private static async Task<Results<Ok, BadRequest<string>, NotFound, UnauthorizedHttpResult>> UpdateAttendanceStatusAsync(Request request, CombinedTimesheetScope scope, Data.Models.Timesheet monthTimesheet, TargetStatus targetStatus, ICurrentUser user, AppDbContext dbContext, NotificationSender notificationSender, CancellationToken cancellationToken)
    {
        Guid currentStatusId = monthTimesheet.TimesheetStatusId;

        if (!TimesheetWorkflow.IsValidAttendanceTransition(monthTimesheet.TimesheetStatus, targetStatus.Code))
        {
            return TypedResults.BadRequest($"Akci '{request.Action}' nelze provést ze stavu '{monthTimesheet.TimesheetStatus.Name}'.");
        }

        if (!user.IsGlobalManagerRole() && user.EmployeeId != monthTimesheet.EmployeeId)
        {
            return TypedResults.Unauthorized();
        }

        bool statusWillChange = currentStatusId != targetStatus.Id;
        if (TimesheetWorkflow.IsSubmitted(targetStatus.Code) && statusWillChange)
        {
            LoadedTimesheet? loaded = await TimesheetEngine.LoadAsync(monthTimesheet.Id, dbContext, cancellationToken);
            if (loaded is null)
            {
                return TypedResults.NotFound();
            }

            TimesheetEvaluation evaluation = TimesheetEngine.Evaluate(loaded, TimesheetEngine.CurrentEditRequest(loaded));
            if (evaluation.HasErrors)
            {
                return TypedResults.BadRequest("Výkaz obsahuje chyby a nelze ho odeslat ke schválení.");
            }
        }

        if (TimesheetWorkflow.IsApproved(targetStatus.Code))
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
            monthTimesheet.TimesheetStatusId = targetStatus.Id;
            monthTimesheet.UpdatedAt = DateTime.UtcNow;

            if (TimesheetWorkflow.IsSubmitted(targetStatus.Code))
            {
                monthTimesheet.SubmittedAt ??= DateTime.UtcNow;
                await SubmitProjectStatusesAsync(scope, user.EmployeeId, request.Comment, dbContext, cancellationToken);
            }
            else if (TimesheetWorkflow.IsApproved(targetStatus.Code))
            {
                monthTimesheet.ApprovedBy = user.EmployeeId;
                monthTimesheet.ApprovedAt = DateTime.UtcNow;
            }
            else if (TimesheetWorkflow.IsDraft(targetStatus.Code))
            {
                monthTimesheet.ApprovedBy = null;
                monthTimesheet.ApprovedAt = null;
                await ResetProjectStatusesAsync(scope, user.EmployeeId, request.Comment, dbContext, cancellationToken);
            }
        }

        if (statusChanged || !string.IsNullOrWhiteSpace(request.Comment))
        {
            TimesheetStatusHistory history = new()
            {
                Id = Guid.CreateVersion7(),
                TimesheetId = monthTimesheet.Id,
                FromStatusId = statusChanged ? currentStatusId : targetStatus.Id,
                ToStatusId = targetStatus.Id,
                ChangedByEmployeeId = user.EmployeeId,
                Comment = request.Comment,
            };

            dbContext.TimesheetStatusHistories.Add(history);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (statusChanged && TimesheetWorkflow.IsSubmitted(targetStatus.Code))
        {
            Guid[] managerIds = await LoadPendingProjectManagerIdsAsync(scope, monthTimesheet.EmployeeId, dbContext, cancellationToken);
            string notificationMessage = BuildApprovalRequestNotificationMessage(monthTimesheet.Employee.DisplayName, request.Year, request.Month);

            foreach (Guid managerId in managerIds)
            {
                await notificationSender.SendAsync(managerId, notificationMessage, cancellationToken);
            }
        }

        return TypedResults.Ok();
    }

    private static async Task<Results<Ok, BadRequest<string>, NotFound, UnauthorizedHttpResult>> UpdateProjectStatusesAsync(Request request, Data.Models.Timesheet monthTimesheet, HashSet<Guid> selectedProjectIds, TargetStatus targetStatus, ICurrentUser user, AppDbContext dbContext, NotificationSender notificationSender, CancellationToken cancellationToken)
    {
        List<ProjectTimesheetPart> projectScopes = await LoadProjectScopesAsync(selectedProjectIds, dbContext, cancellationToken);

        if (projectScopes.Count != selectedProjectIds.Count
            || !user.CanManageProjectTimesheetParts(projectScopes))
        {
            return TypedResults.Unauthorized();
        }

        if (!TimesheetWorkflow.IsSubmitted(monthTimesheet.TimesheetStatus))
        {
            return TypedResults.BadRequest("Projektové sloupce lze schvalovat nebo vracet pouze ve výkazu odeslaném ke schválení.");
        }

        List<Data.Models.ContractPart> projectTimesheets = await dbContext.ContractParts
            .Include(t => t.TimesheetStatus)
            .Where(t => selectedProjectIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        if (projectTimesheets.Count != selectedProjectIds.Count)
        {
            return TypedResults.NotFound();
        }

        bool isProjectReturn = TimesheetWorkflow.IsDraft(targetStatus.Code);
        bool anyProjectStatusChanged = false;

        foreach (Data.Models.ContractPart projectTimesheet in projectTimesheets)
        {
            Guid currentStatusId = projectTimesheet.TimesheetStatusId;

            if (!TimesheetWorkflow.IsValidProjectTransition(projectTimesheet.TimesheetStatus, targetStatus.Code))
            {
                return TypedResults.BadRequest($"Akci '{request.Action}' nelze provést ze stavu '{projectTimesheet.TimesheetStatus.Name}'.");
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
            else if (TimesheetWorkflow.IsApproved(targetStatus.Code))
            {
                projectTimesheet.LockedAt = DateTime.UtcNow;
                projectTimesheet.LockedBy = user.EmployeeId;
            }

            if (statusChanged || !string.IsNullOrWhiteSpace(request.Comment))
            {
                projectTimesheet.UpdatedAt = DateTime.UtcNow;

                TimesheetStatusHistory history = new()
                {
                    Id = Guid.CreateVersion7(),
                    ContractPartId = projectTimesheet.Id,
                    FromStatusId = currentStatusId,
                    ToStatusId = targetStatus.Id,
                    ChangedByEmployeeId = user.EmployeeId,
                    Comment = request.Comment,
                };

                dbContext.TimesheetStatusHistories.Add(history);
            }
        }

        if (isProjectReturn && anyProjectStatusChanged)
        {
            Guid previousAttendanceStatusId = monthTimesheet.TimesheetStatusId;
            monthTimesheet.TimesheetStatusId = TimesheetWorkflow.DraftStatusId;
            monthTimesheet.ApprovedBy = null;
            monthTimesheet.ApprovedAt = null;
            monthTimesheet.UpdatedAt = DateTime.UtcNow;
            dbContext.TimesheetStatusHistories.Add(new TimesheetStatusHistory
            {
                Id = Guid.CreateVersion7(),
                TimesheetId = monthTimesheet.Id,
                FromStatusId = previousAttendanceStatusId,
                ToStatusId = TimesheetWorkflow.DraftStatusId,
                ChangedByEmployeeId = user.EmployeeId,
                Comment = request.Comment,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (anyProjectStatusChanged)
        {
            string notificationMessage = BuildProjectNotificationMessage(request.Year, request.Month, targetStatus.Name, request.Comment);
            await notificationSender.SendAsync(monthTimesheet.EmployeeId, notificationMessage, cancellationToken);
        }

        return TypedResults.Ok();
    }

    private static TargetStatus ResolveTargetStatus(string action) => action switch
    {
        SubmitAction => new TargetStatus(TimesheetWorkflow.SubmittedStatusId, TimesheetStatusCodes.Submitted, TimesheetWorkflow.SubmittedStatusName),
        ApproveAction => new TargetStatus(TimesheetWorkflow.ApprovedStatusId, TimesheetStatusCodes.Approved, TimesheetWorkflow.ApprovedStatusName),
        ReturnAction => new TargetStatus(TimesheetWorkflow.DraftStatusId, TimesheetStatusCodes.Draft, TimesheetWorkflow.DraftStatusName),
        _ => throw new InvalidOperationException($"Unsupported timesheet action '{action}'.")
    };

    private static async Task<bool> AreAllProjectsApprovedAsync(CombinedTimesheetScope scope, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        if (scope.ContractPartLabels.Count == 0)
        {
            return true;
        }

        List<string> projectStatusCodes = await dbContext.ContractParts
            .AsNoTracking()
            .Where(t => scope.ContractPartLabels.Keys.Contains(t.Id))
            .Select(t => t.TimesheetStatus.Code)
            .ToListAsync(cancellationToken);

        return projectStatusCodes.Count == scope.ContractPartLabels.Count
            && projectStatusCodes.All(TimesheetWorkflow.IsApproved);
    }

    private static Task<List<ProjectTimesheetPart>> LoadProjectScopesAsync(IEnumerable<Guid> projectTimesheetIds, AppDbContext dbContext, CancellationToken cancellationToken) => dbContext.ContractParts
        .AsNoTracking()
        .Where(timesheet => projectTimesheetIds.Contains(timesheet.Id))
        .Join(dbContext.ContractEmployees.AsNoTracking(), timesheet => timesheet.ContractEmployeeId, contractEmployee => contractEmployee.Id, (timesheet, contractEmployee) => new { contractEmployee.ContractId })
        .Join(dbContext.Contracts.AsNoTracking(), value => value.ContractId, contract => contract.Id, (value, contract) => new ProjectTimesheetPart(value.ContractId, contract.ProjectId))
        .ToListAsync(cancellationToken);

    private static async Task<Guid[]> LoadPendingProjectManagerIdsAsync(CombinedTimesheetScope scope, Guid employeeId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Guid[] pendingContractPartIds = await dbContext.ContractParts
            .AsNoTracking()
            .Where(timesheet => scope.ContractPartLabels.Keys.Contains(timesheet.Id) && timesheet.TimesheetStatus.Code != TimesheetStatusCodes.Approved)
            .Select(timesheet => timesheet.Id)
            .ToArrayAsync(cancellationToken);

        if (pendingContractPartIds.Length == 0)
        {
            return [];
        }

        List<ProjectTimesheetPart> projectScopes = await LoadProjectScopesAsync(pendingContractPartIds, dbContext, cancellationToken);
        Guid[] contractIds = projectScopes.Select(projectScope => projectScope.ContractId).Distinct().ToArray();
        Guid[] projectIds = projectScopes.Select(projectScope => projectScope.ProjectId).Distinct().ToArray();

        List<Guid> contractManagerIds = await dbContext.ContractManagers
            .AsNoTracking()
            .Where(manager => contractIds.Contains(manager.ContractId) && manager.EmployeeId != employeeId)
            .Select(manager => manager.EmployeeId)
            .ToListAsync(cancellationToken);

        List<Guid> projectManagerIds = await dbContext.ProjectManagers
            .AsNoTracking()
            .Where(manager => projectIds.Contains(manager.ProjectId) && manager.EmployeeId != employeeId)
            .Select(manager => manager.EmployeeId)
            .ToListAsync(cancellationToken);

        return contractManagerIds.Concat(projectManagerIds).Distinct().ToArray();
    }

    private static async Task ResetProjectStatusesAsync(CombinedTimesheetScope scope, Guid changedByEmployeeId, string? comment, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        List<Data.Models.ContractPart> projectTimesheets = await dbContext.ContractParts
            .Where(timesheet => scope.ContractPartLabels.Keys.Contains(timesheet.Id))
            .ToListAsync(cancellationToken);

        foreach (Data.Models.ContractPart projectTimesheet in projectTimesheets)
        {
            if (projectTimesheet.TimesheetStatusId == TimesheetWorkflow.DraftStatusId && projectTimesheet.LockedAt is null)
            {
                continue;
            }

            Guid previousStatusId = projectTimesheet.TimesheetStatusId;
            projectTimesheet.TimesheetStatusId = TimesheetWorkflow.DraftStatusId;
            projectTimesheet.LockedAt = null;
            projectTimesheet.LockedBy = null;
            projectTimesheet.UpdatedAt = DateTime.UtcNow;
            dbContext.TimesheetStatusHistories.Add(new TimesheetStatusHistory
            {
                Id = Guid.CreateVersion7(),
                ContractPartId = projectTimesheet.Id,
                FromStatusId = previousStatusId,
                ToStatusId = TimesheetWorkflow.DraftStatusId,
                ChangedByEmployeeId = changedByEmployeeId,
                Comment = comment,
            });
        }
    }

    private static async Task SubmitProjectStatusesAsync(CombinedTimesheetScope scope, Guid changedByEmployeeId, string? comment, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        List<Data.Models.ContractPart> projectTimesheets = await dbContext.ContractParts
            .Where(timesheet => scope.ContractPartLabels.Keys.Contains(timesheet.Id) && timesheet.TimesheetStatus.Code == TimesheetStatusCodes.Draft)
            .ToListAsync(cancellationToken);

        foreach (Data.Models.ContractPart projectTimesheet in projectTimesheets)
        {
            Guid previousStatusId = projectTimesheet.TimesheetStatusId;
            projectTimesheet.TimesheetStatusId = TimesheetWorkflow.SubmittedStatusId;
            projectTimesheet.UpdatedAt = DateTime.UtcNow;
            dbContext.TimesheetStatusHistories.Add(new TimesheetStatusHistory
            {
                Id = Guid.CreateVersion7(),
                ContractPartId = projectTimesheet.Id,
                FromStatusId = previousStatusId,
                ToStatusId = TimesheetWorkflow.SubmittedStatusId,
                ChangedByEmployeeId = changedByEmployeeId,
                Comment = comment,
            });
        }
    }

    private static string BuildApprovalRequestNotificationMessage(string employeeName, int year, int month)
    {
        string monthName = new DateTime(year, month, 1).ToString("MMMM yyyy", CultureInfo.GetCultureInfo("cs-CZ"));
        return $"Výkaz zaměstnance {employeeName} za {monthName} čeká na schválení projektové části.";
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
