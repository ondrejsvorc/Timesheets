using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Timesheets.Endpoints;

public sealed class DeleteTimesheetComment : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/comments/{commentId}", Handle)
           .WithSummary("Delete Timesheet Comment");

    public sealed record Request([FromQuery] Guid EmployeeId, [FromQuery] int Year, [FromQuery] int Month);
    private sealed record EmployeeMonthScope(Guid TimesheetId, IReadOnlyDictionary<Guid, string> ContractPartLabels);

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> Handle(Guid commentId, [AsParameters] Request request, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!await user.CanAccessEmployeeAsync(request.EmployeeId, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        EmployeeMonthScope? scope = await LoadScopeAsync(request.EmployeeId, request.Year, request.Month, dbContext, cancellationToken);

        if (scope is null)
        {
            return TypedResults.NotFound();
        }

        Domain.Models.TimesheetComment? comment = await dbContext.TimesheetComments.FirstOrDefaultAsync(entry => entry.Id == commentId, cancellationToken);

        if (comment is null)
        {
            return TypedResults.NotFound();
        }

        if (comment.AuthorEmployeeId != user.EmployeeId)
        {
            return TypedResults.Forbid();
        }

        IReadOnlyList<Guid> contractPartIds = scope.ContractPartLabels.Keys.ToList();
        bool isAttendanceComment = comment.TimesheetId == scope.TimesheetId;
        bool isProjectComment = comment.ContractPartId is not null && contractPartIds.Contains(comment.ContractPartId.Value);

        if (!isAttendanceComment && !isProjectComment)
        {
            return TypedResults.NotFound();
        }

        dbContext.TimesheetComments.Remove(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
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
}
