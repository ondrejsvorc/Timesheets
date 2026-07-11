using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Projects.Endpoints;

public sealed class GetProjectContracts : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}/contracts", Handle)
           .WithSummary("Get Project Contracts");

    public sealed record Response(IEnumerable<ProjectContractItem> ProjectContracts);

    private static async Task<Results<Ok<Response>, ForbidHttpResult>> Handle(Guid id, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.Satisfies(UserRole.Employee, projectId: id))
        {
            return TypedResults.Forbid();
        }

        List<ProjectContractItem> contracts = await dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.ProjectId == id)
            .Select(c => new ProjectContractItem(
                c.Id,
                c.Name,
                c.RegistrationNumber,
                c.ContractEmployees.Count
            ))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(contracts));
    }
}
