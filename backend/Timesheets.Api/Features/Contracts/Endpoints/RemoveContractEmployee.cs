using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Features.Auth;
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

        bool exists = await dbContext.ContractEmployees
            .AsNoTracking()
            .AnyAsync(contractEmployee => contractEmployee.ContractId == id && contractEmployee.Id == contractEmployeeId, cancellationToken);

        if (!exists)
        {
            return TypedResults.NotFound();
        }

        bool hasProtectedTimesheets = await dbContext.ProjectTimesheets
            .AsNoTracking()
            .AnyAsync(timesheet => timesheet.ContractEmployeeId == contractEmployeeId && (timesheet.TimesheetStatus.Code == TimesheetStatusCodes.Submitted || timesheet.TimesheetStatus.Code == TimesheetStatusCodes.Approved), cancellationToken);
        if (hasProtectedTimesheets)
        {
            return TypedResults.Conflict("Pozici nelze odebrat, protože obsahuje výkazy ke schválení nebo schválené.");
        }

        await dbContext.ContractEmployees
            .Where(contractEmployee => contractEmployee.ContractId == id && contractEmployee.Id == contractEmployeeId)
            .ExecuteDeleteAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
