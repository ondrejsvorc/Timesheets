using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;
using Timesheets.Api.Domain.Models;
using Timesheets.Api.Features.Auth;
using Timesheets.Api.Features.Projects;

namespace Timesheets.Api.Features.Projects.Endpoints;

public sealed class DeleteProject : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/{id}", Handle)
           .WithSummary("Delete Project");

    private static async Task<Results<NoContent, NotFound, Conflict<string>, ForbidHttpResult>> Handle(Guid id, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.IsGlobalManagerRole())
        {
            return TypedResults.Forbid();
        }

        bool exists = await dbContext.Projects.AsNoTracking().AnyAsync(p => p.Id == id, cancellationToken);
        if (!exists)
        {
            return TypedResults.NotFound();
        }

        if (await ProjectArchiveGuard.BlockIfArchivedAsync(id, dbContext, cancellationToken) is { } archiveBlock)
        {
            return TypedResults.Conflict(archiveBlock);
        }

        List<Guid> contractIds = await dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.ProjectId == id)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        if (await DeleteImpactCore.HasProtectedTimesheetsAsync(contractIds, dbContext, cancellationToken))
        {
            return TypedResults.Conflict("Projekt nelze smazat, protože obsahuje výkazy ke schválení nebo schválené.");
        }

        await RemoveDraftPartsForContractIdsAsync(contractIds, dbContext, cancellationToken);

        await dbContext.Projects
            .Where(p => p.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task RemoveDraftPartsForContractIdsAsync(IReadOnlyList<Guid> contractIds, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        if (contractIds.Count == 0)
        {
            return;
        }

        List<Guid> assignmentIds = await dbContext.ContractEmployees
            .AsNoTracking()
            .Where(assignment => contractIds.Contains(assignment.ContractId))
            .Select(assignment => assignment.Id)
            .ToListAsync(cancellationToken);

        foreach (Guid assignmentId in assignmentIds)
        {
            await RemoveDraftPartsForAssignmentAsync(assignmentId, dbContext, cancellationToken);
        }
    }

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
