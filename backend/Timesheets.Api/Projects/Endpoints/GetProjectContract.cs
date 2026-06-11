using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Auth;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class GetProjectContract : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{projectId}/contracts/{contractId}", Handle)
           .WithSummary("Get Project Contract");

    public sealed record Response(Guid Id, string Name, string RegistrationNumber);

    private static async Task<Results<Ok<Response>, NotFound, ForbidHttpResult>> Handle(
        Guid projectId,
        Guid contractId,
        AppDbContext dbContext,
        ICurrentUser user,
        CancellationToken cancellationToken)
    {
        if (!user.Satisfies(UserRole.Employee, contractId: contractId))
        {
            return TypedResults.Forbid();
        }

        Response? contract = await dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.ProjectId == projectId && c.Id == contractId)
            .Select(c => new Response(
                c.Id,
                c.Name,
                c.RegistrationNumber
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (contract is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(contract);
    }
}
