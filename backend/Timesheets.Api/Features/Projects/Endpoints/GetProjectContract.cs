using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Projects.Endpoints;

public sealed class GetProjectContract : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{contractEmployeeId}/contracts/{contractId}", Handle)
           .WithSummary("Get Project Contract");

    public sealed record Response(Guid Id, string Name, string RegistrationNumber, DateTime ProjectStartDate, DateTime? ProjectEndDate);

    private static async Task<Results<Ok<Response>, NotFound, ForbidHttpResult>> Handle(Guid contractEmployeeId, Guid contractId, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.Satisfies(UserRole.Employee, contractId: contractId))
        {
            return TypedResults.Forbid();
        }

        Response? contract = await dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.ProjectId == contractEmployeeId && c.Id == contractId)
            .Select(c => new Response(
                c.Id,
                c.Name,
                c.RegistrationNumber,
                c.Project.StartDate,
                c.Project.EndDate
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (contract is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(contract);
    }
}
