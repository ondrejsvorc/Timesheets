using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;
using Timesheets.Api.Features.Auth;
using Timesheets.Api.Features.Projects;

namespace Timesheets.Api.Features.Contracts.Endpoints;

public sealed class RemoveContractManager : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/{id}/managers/{employeeId}", Handle)
           .WithSummary("Remove Manager from Contract");

    private static async Task<Results<NoContent, NotFound, Conflict<string>, ForbidHttpResult>> Handle(Guid id, Guid employeeId, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.Satisfies(UserRole.ProjectManager, contractId: id))
        {
            return TypedResults.Forbid();
        }

        if (await ProjectArchiveGuard.BlockIfContractArchivedAsync(id, dbContext, cancellationToken) is { } archiveBlock)
        {
            return TypedResults.Conflict(archiveBlock);
        }

        int affected = await dbContext.ContractManagers
            .Where(cm => cm.ContractId == id && cm.EmployeeId == employeeId)
            .ExecuteDeleteAsync(cancellationToken);

        if (affected == 0)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }
}
