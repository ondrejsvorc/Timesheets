using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Contracts.Endpoints;

public sealed class RemoveContractManager : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/{id}/managers/{employeeId}", Handle)
           .WithSummary("Remove Manager from Contract");

    private static async Task<Results<NoContent, NotFound>> Handle(Guid id, Guid employeeId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
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
