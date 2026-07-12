using System.Globalization;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Domain;
using Timesheets.Api.Domain.Models;
using Timesheets.Api.Features.Auth;
using Timesheets.Api.Features.Notifications;

namespace Timesheets.Api.Features.Timesheets.Endpoints;

public sealed class UpdateTimesheetStatus : IEndpoint
{
    private const string SubmitAction = "submit";
    private const string ApproveAction = "approve";
    private const string ReturnAction = "return";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/status", Handle)
           .WithSummary("Update Timesheet Status")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request(Guid EmployeeId, int Year, int Month, string Action, string? Comment, IReadOnlyList<Guid> TimesheetIds, TimesheetEdit? Draft = null);
    private sealed record TargetStatus(Guid Id, string Code, string Name);
    private sealed record EmployeeMonthScope(Guid TimesheetId, IReadOnlyDictionary<Guid, string> ContractPartLabels);

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

    private static async Task<Results<Ok, BadRequest<string>, NotFound, UnauthorizedHttpResult>> Handle([FromBody] Request request, AppDbContext dbContext, NotificationSender notificationSender, ICurrentUser user, TimesheetEvaluator evaluator, CancellationToken cancellationToken)
    {
        EmployeeMonthScope? scope = await LoadScopeAsync(request.EmployeeId, request.Year, request.Month, dbContext, cancellationToken);

        if (scope is null)
        {
            return TypedResults.NotFound();
        }

        Domain.Models.Timesheet? monthTimesheet = await dbContext.Timesheets
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
        HashSet<Guid> selectedContractPartIds = selectedIds
            .Where(id => scope.ContractPartLabels.ContainsKey(id))
            .ToHashSet();

        if (includesAttendance && selectedContractPartIds.Count > 0)
        {
            return TypedResults.BadRequest("Nelze měnit stav docházky a projektových výkazů v jednom požadavku.");
        }

        if (!includesAttendance && selectedContractPartIds.Count == 0)
        {
            return TypedResults.BadRequest("One or more selected timesheets are invalid for this employee and period.");
        }

        if (selectedIds.Any(id => id != scope.TimesheetId && !scope.ContractPartLabels.ContainsKey(id)))
        {
            return TypedResults.BadRequest("One or more selected timesheets are invalid for this employee and period.");
        }

        if (includesAttendance)
        {
            return await UpdateAttendanceStatusAsync(request, scope, monthTimesheet, targetStatus, user, dbContext, notificationSender, evaluator, cancellationToken);
        }

        return await UpdateProjectStatusesAsync(request, monthTimesheet, selectedContractPartIds, targetStatus, user, dbContext, notificationSender, cancellationToken);
    }

    private static async Task<EmployeeMonthScope?> LoadScopeAsync(Guid employeeId, int year, int month, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Guid? timesheetId = await dbContext.Timesheets
            .AsNoTracking()
            .Where(timesheet => timesheet.EmployeeId == employeeId && timesheet.Year == year && timesheet.Month == month)
            .Select(timesheet => (Guid?)timesheet.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (timesheetId is null)
        {
            return null;
        }

        List<(Guid Id, string ContractRegistrationNumber)> contractPartRows = await dbContext.ContractParts
            .AsNoTracking()
            .Where(part => part.TimesheetId == timesheetId.Value)
            .Join(dbContext.ContractEmployees.AsNoTracking(), timesheet => timesheet.ContractEmployeeId, contractEmployee => contractEmployee.Id, (timesheet, contractEmployee) => new { timesheet, contractEmployee })
            .Join(dbContext.Contracts.AsNoTracking(), x => x.contractEmployee.ContractId, contract => contract.Id, (x, contract) => new { x.timesheet.Id, contract.RegistrationNumber })
            .OrderBy(x => x.RegistrationNumber)
            .Select(x => new ValueTuple<Guid, string>(x.Id, x.RegistrationNumber))
            .ToListAsync(cancellationToken);

        Dictionary<Guid, string> labels = contractPartRows.ToDictionary(row => row.Id, row => row.ContractRegistrationNumber);

        return new EmployeeMonthScope(timesheetId.Value, labels);
    }

    private static async Task<Results<Ok, BadRequest<string>, NotFound, UnauthorizedHttpResult>> UpdateAttendanceStatusAsync(Request request, EmployeeMonthScope scope, Domain.Models.Timesheet monthTimesheet, TargetStatus targetStatus, ICurrentUser user, AppDbContext dbContext, NotificationSender notificationSender, TimesheetEvaluator evaluator, CancellationToken cancellationToken)
    {
        Guid currentStatusId = monthTimesheet.TimesheetStatusId;

        if (!TimesheetStatus.IsValidAttendanceTransition(monthTimesheet.TimesheetStatus, targetStatus.Code))
        {
            return TypedResults.BadRequest($"Akci '{request.Action}' nelze provést ze stavu '{monthTimesheet.TimesheetStatus.Name}'.");
        }

        if (!user.IsGlobalManagerRole() && user.EmployeeId != monthTimesheet.EmployeeId)
        {
            return TypedResults.Unauthorized();
        }

        bool statusWillChange = currentStatusId != targetStatus.Id;
        if (targetStatus.Code == TimesheetStatus.SubmittedCode && statusWillChange)
        {
            LoadedTimesheet? loaded = await LoadAsync(monthTimesheet.Id, dbContext, cancellationToken);
            if (loaded is null)
            {
                return TypedResults.NotFound();
            }

            if (request.Draft is not null)
            {
                if (evaluator.HasInactiveContractPartHours(loaded, request.Draft))
                {
                    return TypedResults.BadRequest("Zakázkové hodiny nelze vyplnit mimo platnost pozice nebo projektu.");
                }

                UpdateTimesheet.ApplyEdits(loaded, request.Draft);
            }

            TimesheetEvaluation evaluation = evaluator.Evaluate(loaded, request.Draft ?? evaluator.CurrentEdit(loaded));
            if (evaluation.HasErrors)
            {
                return TypedResults.BadRequest("Výkaz obsahuje chyby a nelze ho odeslat ke schválení.");
            }

            if (request.Draft is not null)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        if (targetStatus.Code == TimesheetStatus.ApprovedCode)
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

            if (targetStatus.Code == TimesheetStatus.SubmittedCode)
            {
                monthTimesheet.SubmittedAt ??= DateTime.UtcNow;
                await SubmitProjectStatusesAsync(scope, user.EmployeeId, request.Comment, dbContext, cancellationToken);
            }
            else if (targetStatus.Code == TimesheetStatus.ApprovedCode)
            {
                monthTimesheet.ApprovedBy = user.EmployeeId;
                monthTimesheet.ApprovedAt = DateTime.UtcNow;
            }
            else if (targetStatus.Code == TimesheetStatus.DraftCode)
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

        if (statusChanged && targetStatus.Code == TimesheetStatus.SubmittedCode)
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

    private static async Task<Results<Ok, BadRequest<string>, NotFound, UnauthorizedHttpResult>> UpdateProjectStatusesAsync(Request request, Domain.Models.Timesheet monthTimesheet, HashSet<Guid> selectedContractPartIds, TargetStatus targetStatus, ICurrentUser user, AppDbContext dbContext, NotificationSender notificationSender, CancellationToken cancellationToken)
    {
        List<ContractPartScope> contractPartScopes = await LoadContractPartScopesAsync(selectedContractPartIds, dbContext, cancellationToken);

        if (contractPartScopes.Count != selectedContractPartIds.Count
            || !user.CanManageContractPartScopes(contractPartScopes))
        {
            return TypedResults.Unauthorized();
        }

        if (!monthTimesheet.TimesheetStatus.IsSubmitted)
        {
            return TypedResults.BadRequest("Projektové sloupce lze schvalovat nebo vracet pouze ve výkazu odeslaném ke schválení.");
        }

        List<Domain.Models.ContractPart> contractParts = await dbContext.ContractParts
            .Include(t => t.TimesheetStatus)
            .Where(t => selectedContractPartIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        if (contractParts.Count != selectedContractPartIds.Count)
        {
            return TypedResults.NotFound();
        }

        bool isProjectReturn = targetStatus.Code == TimesheetStatus.DraftCode;
        bool anyProjectStatusChanged = false;

        foreach (Domain.Models.ContractPart projectTimesheet in contractParts)
        {
            Guid currentStatusId = projectTimesheet.TimesheetStatusId;

            if (!TimesheetStatus.IsValidProjectTransition(projectTimesheet.TimesheetStatus, targetStatus.Code))
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
            else if (targetStatus.Code == TimesheetStatus.ApprovedCode)
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
            monthTimesheet.TimesheetStatusId = TimesheetStatus.DraftId;
            monthTimesheet.ApprovedBy = null;
            monthTimesheet.ApprovedAt = null;
            monthTimesheet.UpdatedAt = DateTime.UtcNow;
            dbContext.TimesheetStatusHistories.Add(new TimesheetStatusHistory
            {
                Id = Guid.CreateVersion7(),
                TimesheetId = monthTimesheet.Id,
                FromStatusId = previousAttendanceStatusId,
                ToStatusId = TimesheetStatus.DraftId,
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
        SubmitAction => new TargetStatus(TimesheetStatus.SubmittedId, TimesheetStatus.SubmittedCode, TimesheetStatus.SubmittedName),
        ApproveAction => new TargetStatus(TimesheetStatus.ApprovedId, TimesheetStatus.ApprovedCode, TimesheetStatus.ApprovedName),
        ReturnAction => new TargetStatus(TimesheetStatus.DraftId, TimesheetStatus.DraftCode, TimesheetStatus.DraftName),
        _ => throw new InvalidOperationException($"Unsupported timesheet action '{action}'.")
    };

    private static async Task<bool> AreAllProjectsApprovedAsync(EmployeeMonthScope scope, AppDbContext dbContext, CancellationToken cancellationToken)
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
            && projectStatusCodes.All(code => code == TimesheetStatus.ApprovedCode);
    }

    private static Task<List<ContractPartScope>> LoadContractPartScopesAsync(IEnumerable<Guid> contractPartIds, AppDbContext dbContext, CancellationToken cancellationToken) => dbContext.ContractParts
        .AsNoTracking()
        .Where(timesheet => contractPartIds.Contains(timesheet.Id))
        .Join(dbContext.ContractEmployees.AsNoTracking(), timesheet => timesheet.ContractEmployeeId, contractEmployee => contractEmployee.Id, (timesheet, contractEmployee) => new { contractEmployee.ContractId })
        .Join(dbContext.Contracts.AsNoTracking(), value => value.ContractId, contract => contract.Id, (value, contract) => new ContractPartScope(value.ContractId, contract.ProjectId))
        .ToListAsync(cancellationToken);

    private static async Task<Guid[]> LoadPendingProjectManagerIdsAsync(EmployeeMonthScope scope, Guid employeeId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Guid[] pendingContractPartIds = await dbContext.ContractParts
            .AsNoTracking()
            .Where(timesheet => scope.ContractPartLabels.Keys.Contains(timesheet.Id) && timesheet.TimesheetStatus.Code != TimesheetStatus.ApprovedCode)
            .Select(timesheet => timesheet.Id)
            .ToArrayAsync(cancellationToken);

        if (pendingContractPartIds.Length == 0)
        {
            return [];
        }

        List<ContractPartScope> contractPartScopes = await LoadContractPartScopesAsync(pendingContractPartIds, dbContext, cancellationToken);
        Guid[] contractIds = contractPartScopes.Select(projectScope => projectScope.ContractId).Distinct().ToArray();
        Guid[] contractEmployeeIds = contractPartScopes.Select(projectScope => projectScope.ProjectId).Distinct().ToArray();

        List<Guid> contractManagerIds = await dbContext.ContractManagers
            .AsNoTracking()
            .Where(manager => contractIds.Contains(manager.ContractId) && manager.EmployeeId != employeeId)
            .Select(manager => manager.EmployeeId)
            .ToListAsync(cancellationToken);

        List<Guid> projectManagerIds = await dbContext.ProjectManagers
            .AsNoTracking()
            .Where(manager => contractEmployeeIds.Contains(manager.ProjectId) && manager.EmployeeId != employeeId)
            .Select(manager => manager.EmployeeId)
            .ToListAsync(cancellationToken);

        return contractManagerIds.Concat(projectManagerIds).Distinct().ToArray();
    }

    private static async Task ResetProjectStatusesAsync(EmployeeMonthScope scope, Guid changedByEmployeeId, string? comment, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        List<Domain.Models.ContractPart> contractParts = await dbContext.ContractParts
            .Where(timesheet => scope.ContractPartLabels.Keys.Contains(timesheet.Id))
            .ToListAsync(cancellationToken);

        foreach (Domain.Models.ContractPart projectTimesheet in contractParts)
        {
            if (projectTimesheet.TimesheetStatusId == TimesheetStatus.DraftId && projectTimesheet.LockedAt is null)
            {
                continue;
            }

            Guid previousStatusId = projectTimesheet.TimesheetStatusId;
            projectTimesheet.TimesheetStatusId = TimesheetStatus.DraftId;
            projectTimesheet.LockedAt = null;
            projectTimesheet.LockedBy = null;
            projectTimesheet.UpdatedAt = DateTime.UtcNow;
            dbContext.TimesheetStatusHistories.Add(new TimesheetStatusHistory
            {
                Id = Guid.CreateVersion7(),
                ContractPartId = projectTimesheet.Id,
                FromStatusId = previousStatusId,
                ToStatusId = TimesheetStatus.DraftId,
                ChangedByEmployeeId = changedByEmployeeId,
                Comment = comment,
            });
        }
    }

    private static async Task SubmitProjectStatusesAsync(EmployeeMonthScope scope, Guid changedByEmployeeId, string? comment, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        List<Domain.Models.ContractPart> contractParts = await dbContext.ContractParts
            .Where(timesheet => scope.ContractPartLabels.Keys.Contains(timesheet.Id) && timesheet.TimesheetStatus.Code == TimesheetStatus.DraftCode)
            .ToListAsync(cancellationToken);

        foreach (Domain.Models.ContractPart projectTimesheet in contractParts)
        {
            Guid previousStatusId = projectTimesheet.TimesheetStatusId;
            projectTimesheet.TimesheetStatusId = TimesheetStatus.SubmittedId;
            projectTimesheet.UpdatedAt = DateTime.UtcNow;
            dbContext.TimesheetStatusHistories.Add(new TimesheetStatusHistory
            {
                Id = Guid.CreateVersion7(),
                ContractPartId = projectTimesheet.Id,
                FromStatusId = previousStatusId,
                ToStatusId = TimesheetStatus.SubmittedId,
                ChangedByEmployeeId = changedByEmployeeId,
                Comment = comment,
            });
        }
    }

    private static async Task<LoadedTimesheet?> LoadAsync(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Domain.Models.Timesheet? timesheet = await dbContext.Timesheets
            .Include(value => value.Employee)
            .Include(value => value.TimesheetStatus)
            .SingleOrDefaultAsync(value => value.Id == id, cancellationToken);

        if (timesheet is null)
        {
            return null;
        }

        Domain.Models.Attendance? attendance = await dbContext.Attendances
            .Include(value => value.Days)
            .SingleOrDefaultAsync(value => value.TimesheetId == id, cancellationToken);

        if (attendance is null)
        {
            return null;
        }

        List<Domain.Models.ContractPart> projects = await dbContext.ContractParts
            .Include(value => value.Days)
            .Where(value => value.TimesheetId == timesheet.Id)
            .ToListAsync(cancellationToken);

        Guid[] assignmentIds = projects.Select(project => project.ContractEmployeeId).ToArray();
        var rangeRows = await (
            from assignment in dbContext.ContractEmployees.AsNoTracking()
            join contract in dbContext.Contracts.AsNoTracking() on assignment.ContractId equals contract.Id
            join project in dbContext.Projects.AsNoTracking() on contract.ProjectId equals project.Id
            where assignmentIds.Contains(assignment.Id)
            select new
            {
                assignment.Id,
                assignment.StartDate,
                AssignmentEndDate = assignment.EndDate,
                ProjectStartDate = project.StartDate,
                ProjectEndDate = project.EndDate
            })
            .ToListAsync(cancellationToken);
        Dictionary<Guid, ContractPartDateRange> projectRanges = rangeRows.ToDictionary(
            row => row.Id,
            row => EffectiveContractPartRange(row.StartDate, row.AssignmentEndDate, row.ProjectStartDate, row.ProjectEndDate));

        decimal totalWorkload = await GetWorkloadAsync(timesheet.EmployeeId, timesheet.Year, timesheet.Month, dbContext, cancellationToken);
        decimal coreWorkload = Math.Max(0m, totalWorkload - projects.Sum(project => project.Workload));
        return new LoadedTimesheet(Timesheet: timesheet, Attendance: attendance, ContractParts: projects, ContractPartRanges: projectRanges, TotalWorkload: totalWorkload, CoreWorkload: coreWorkload);
    }

    private static async Task<decimal> GetWorkloadAsync(Guid employeeId, int year, int month, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        decimal? monthly = await dbContext.EmployeeWorkloads
            .AsNoTracking()
            .Where(workload => workload.EmployeeId == employeeId && workload.Year == year && workload.Month == month)
            .Select(workload => (decimal?)workload.Workload)
            .FirstOrDefaultAsync(cancellationToken);

        if (monthly.HasValue)
        {
            return monthly.Value;
        }

        DateTime periodStart = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime periodEnd = periodStart.AddMonths(1).AddDays(-1);

        return await dbContext.CoreEmployments
            .AsNoTracking()
            .Where(employment => employment.EmployeeId == employeeId)
            .Where(employment => employment.StartDate <= periodEnd && (employment.EndDate == null || employment.EndDate >= periodStart))
            .OrderByDescending(employment => employment.StartDate)
            .Select(employment => (decimal?)employment.Workload)
            .FirstOrDefaultAsync(cancellationToken) ?? 0m;
    }

    private static ContractPartDateRange EffectiveContractPartRange(DateTime assignmentStartDate, DateTime? assignmentEndDate, DateTime projectStartDate, DateTime? projectEndDate)
    {
        DateTime start = Max(ToUtcDate(assignmentStartDate), ToUtcDate(projectStartDate));
        DateTime? end = Min(assignmentEndDate.HasValue ? ToUtcDate(assignmentEndDate.Value) : null, projectEndDate.HasValue ? ToUtcDate(projectEndDate.Value) : null);
        return new ContractPartDateRange(start, end);
    }

    private static DateTime Max(DateTime first, DateTime second) => first >= second ? first : second;

    private static DateTime? Min(DateTime? first, DateTime? second) => (first, second) switch
    {
        (null, null) => null,
        (DateTime value, null) => value,
        (null, DateTime value) => value,
        (DateTime left, DateTime right) => left <= right ? left : right
    };

    private static DateTime ToUtcDate(DateTime value) => value.Kind == DateTimeKind.Utc ? value.Date : DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

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
