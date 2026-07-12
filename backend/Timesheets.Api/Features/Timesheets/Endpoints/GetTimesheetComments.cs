using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;
using Timesheets.Api.Domain.Models;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Timesheets.Endpoints;

public sealed class GetTimesheetComments : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/comments", Handle)
           .WithSummary("Get Timesheet Comments");

    public sealed record Request([FromQuery] Guid EmployeeId, [FromQuery] int Year, [FromQuery] int Month);
    public sealed record CommentAuthor(Guid Id, string Name);
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
    private sealed record EmployeeMonthScope(Guid TimesheetId, IReadOnlyDictionary<Guid, string> ContractPartLabels);

    private static async Task<Results<Ok<IReadOnlyList<CommentItem>>, NotFound, ForbidHttpResult>> Handle([AsParameters] Request request, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!await user.CanAccessEmployeeAsync(request.EmployeeId, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        EmployeeMonthScope? scope = await LoadScopeAsync(
            request.EmployeeId,
            request.Year,
            request.Month,
            dbContext,
            cancellationToken);

        if (scope is null)
        {
            return TypedResults.NotFound();
        }

        IReadOnlyList<Guid> contractEmployeeIds = scope.ContractPartLabels.Keys.ToList();

        List<Domain.Models.TimesheetComment> comments = await dbContext.TimesheetComments
            .AsNoTracking()
            .Include(c => c.AuthorEmployee)
            .Where(c =>
                c.TimesheetId == scope.TimesheetId
                || (c.ContractPartId != null && contractEmployeeIds.Contains(c.ContractPartId.Value)))
            .ToListAsync(cancellationToken);

        List<TimesheetStatusHistory> history = await dbContext.TimesheetStatusHistories
            .AsNoTracking()
            .Include(h => h.FromStatus)
            .Include(h => h.ToStatus)
            .Include(h => h.ChangedByEmployee)
            .Where(h =>
                h.TimesheetId == scope.TimesheetId
                || (h.ContractPartId != null && contractEmployeeIds.Contains(h.ContractPartId.Value)))
            .ToListAsync(cancellationToken);

        List<CommentItem> items = comments
            .Select(comment => new CommentItem(
                comment.Id,
                "message",
                comment.CreatedAt,
                comment.Text,
                new CommentAuthor(
                    comment.AuthorEmployeeId,
                    comment.AuthorEmployee.DisplayName),
                null))
            .Concat(history.Select(entry => new CommentItem(
                entry.Id,
                "statusChange",
                entry.ChangedAt,
                null,
                null,
                new StatusChangeDetails(
                    new CommentAuthor(
                        entry.ChangedByEmployeeId,
                        entry.ChangedByEmployee.DisplayName),
                    ResolveTimesheetLabel(scope, entry.TimesheetId, entry.ContractPartId),
                    entry.FromStatus?.Name,
                    entry.ToStatus.Name,
                    entry.Comment))))
            .OrderBy(item => item.CreatedAt)
            .ToList();

        return TypedResults.Ok<IReadOnlyList<CommentItem>>(items);
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

    private static string ResolveTimesheetLabel(EmployeeMonthScope scope, Guid? timesheetId, Guid? contractPartId)
    {
        if (timesheetId is not null)
        {
            return "Pracovní výkaz";
        }

        if (contractPartId is not null && scope.ContractPartLabels.TryGetValue(contractPartId.Value, out string? label))
        {
            return label;
        }

        return "Výkaz";
    }
}
