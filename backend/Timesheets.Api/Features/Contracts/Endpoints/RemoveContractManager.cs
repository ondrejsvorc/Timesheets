using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Contracts.Endpoints;

public sealed class RemoveContractManager : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/{id}/managers/{employeeId}", Handle)
           .WithSummary("Remove Manager from Contract");

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> Handle(Guid id, Guid employeeId, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.Satisfies(UserRole.ProjectManager, contractId: id))
        {
            return TypedResults.Forbid();
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
