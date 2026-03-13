using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;

namespace Timesheets.Api.Contracts.Endpoints;

public sealed class RemoveContractEmployee : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/{id}/employees/{employeeId}", Handle)
           .WithSummary("Remove Employee from Contract")
           .WithRequestValidation<Request>();

    private static async Task<Results<NoContent, NotFound>> Handle(Guid id, Guid employeeId, [FromQuery] string position, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        int affected = await dbContext.ContractEmployees
            .Where(ce => ce.ContractId == id && ce.EmployeeId == employeeId && ce.Position == position)
            .ExecuteDeleteAsync(cancellationToken);

        if (affected == 0)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }
}
