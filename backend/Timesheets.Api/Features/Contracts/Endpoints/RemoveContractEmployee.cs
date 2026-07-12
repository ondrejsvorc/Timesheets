using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;
using Timesheets.Api.Domain.Models;
using Timesheets.Api.Features.Auth;
using Timesheets.Api.Features.Projects;

namespace Timesheets.Api.Features.Contracts.Endpoints;

public sealed class RemoveContractEmployee : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/{id}/employees/{contractEmployeeId}", Handle)
           .WithSummary("Remove Employee Position from Contract");

    private static async Task<Results<NoContent, NotFound, Conflict<string>, ForbidHttpResult>> Handle(Guid id, Guid contractEmployeeId, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.Satisfies(UserRole.ContractManager, contractId: id))
        {
            return TypedResults.Forbid();
        }

        if (await ProjectArchiveGuard.BlockIfContractArchivedAsync(id, dbContext, cancellationToken) is { } archiveBlock)
        {
            return TypedResults.Conflict(archiveBlock);
        }

        bool exists = await dbContext.ContractEmployees
            .AsNoTracking()
            .AnyAsync(contractEmployee => contractEmployee.ContractId == id && contractEmployee.Id == contractEmployeeId, cancellationToken);

        if (!exists)
        {
            return TypedResults.NotFound();
        }

        if (await HasProtectedPartsForAssignmentAsync(contractEmployeeId, dbContext, cancellationToken))
        {
            return TypedResults.Conflict("Pozici nelze odebrat, protože obsahuje výkazy ke schválení nebo schválené.");
        }

        await RemoveDraftPartsForAssignmentAsync(contractEmployeeId, dbContext, cancellationToken);

        await dbContext.ContractEmployees
            .Where(contractEmployee => contractEmployee.ContractId == id && contractEmployee.Id == contractEmployeeId)
            .ExecuteDeleteAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<bool> HasProtectedPartsForAssignmentAsync(Guid contractEmployeeId, AppDbContext dbContext, CancellationToken cancellationToken) =>
        await dbContext.ContractParts
            .AsNoTracking()
            .AnyAsync(
                part => part.ContractEmployeeId == contractEmployeeId
                    && (part.TimesheetStatus.Code == TimesheetStatus.SubmittedCode || part.TimesheetStatus.Code == TimesheetStatus.ApprovedCode),
                cancellationToken);

    private static async Task RemoveDraftPartsForAssignmentAsync(Guid contractEmployeeId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        List<Guid> draftPartIds = await dbContext.ContractParts
            .Where(part => part.ContractEmployeeId == contractEmployeeId && part.TimesheetStatus.Code == TimesheetStatus.DraftCode)
            .Select(part => part.Id)
            .ToListAsync(cancellationToken);

        if (draftPartIds.Count == 0)
        {
            return;
        }

        HashSet<Guid> timesheetIds = await dbContext.ContractParts
            .AsNoTracking()
            .Where(part => draftPartIds.Contains(part.Id))
            .Select(part => part.TimesheetId)
            .ToHashSetAsync(cancellationToken);

        await dbContext.ContractParts
            .Where(part => draftPartIds.Contains(part.Id))
            .ExecuteDeleteAsync(cancellationToken);

        await CleanupEmptyDraftTimesheetsAsync(timesheetIds, dbContext, cancellationToken);
    }

    private static async Task CleanupEmptyDraftTimesheetsAsync(HashSet<Guid> timesheetIds, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        foreach (Guid timesheetId in timesheetIds)
        {
            bool hasParts = await dbContext.ContractParts.AnyAsync(part => part.TimesheetId == timesheetId, cancellationToken);
            if (hasParts)
            {
                continue;
            }

            bool hasAttendance = await dbContext.Attendances.AnyAsync(attendance => attendance.TimesheetId == timesheetId, cancellationToken);
            if (hasAttendance)
            {
                continue;
            }

            bool isDraft = await dbContext.Timesheets
                .AsNoTracking()
                .AnyAsync(
                    timesheet => timesheet.Id == timesheetId && timesheet.TimesheetStatus.Code == TimesheetStatus.DraftCode,
                    cancellationToken);

            if (!isDraft)
            {
                continue;
            }

            await dbContext.Timesheets
                .Where(timesheet => timesheet.Id == timesheetId)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
