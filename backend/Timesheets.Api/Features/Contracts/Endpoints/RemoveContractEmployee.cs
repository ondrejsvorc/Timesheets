using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;
using Timesheets.Api.Features.Auth;
using Timesheets.Api.Features.Projects;
using Timesheets.Api.Features.Timesheets;

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

        if (await ContractPartCleanup.HasProtectedPartsForAssignmentAsync(contractEmployeeId, dbContext, cancellationToken))
        {
            return TypedResults.Conflict("Pozici nelze odebrat, protože obsahuje výkazy ke schválení nebo schválené.");
        }

        await ContractPartCleanup.RemoveDraftPartsForAssignmentAsync(contractEmployeeId, dbContext, cancellationToken);

        await dbContext.ContractEmployees
            .Where(contractEmployee => contractEmployee.ContractId == id && contractEmployee.Id == contractEmployeeId)
            .ExecuteDeleteAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
