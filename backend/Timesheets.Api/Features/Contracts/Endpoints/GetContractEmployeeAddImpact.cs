using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;
using Timesheets.Api.Features.Auth;
using Timesheets.Api.Features.Projects;

namespace Timesheets.Api.Features.Contracts.Endpoints;

public sealed class GetContractEmployeeAddImpact : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/{id}/employees/add-impact", Handle)
           .WithSummary("Get Contract Employee Add Impact")
           .DisableAntiforgery();

    public sealed record Request(Guid EmployeeId, DateTime StartDate, DateTime? EndDate);

    private static async Task<Results<Ok<ContractEmployeeAddImpact>, NotFound, ForbidHttpResult>> Handle(Guid id, [FromBody] Request request, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.Satisfies(UserRole.ContractManager, contractId: id))
        {
            return TypedResults.Forbid();
        }

        var contract = await dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new { c.Project.EndDate, c.Project.ArchivedAt })
            .SingleOrDefaultAsync(cancellationToken);
        if (contract is null)
        {
            return TypedResults.NotFound();
        }

        if (contract.ArchivedAt.HasValue)
        {
            return TypedResults.Ok(new ContractEmployeeAddImpact(false, ProjectArchiveGuard.BlockMessage, 0, 0));
        }

        ContractEmployeeAddImpact impact = await ContractEmployeeAddPlanner.PlanAsync(
            id,
            contract.EndDate,
            new ContractEmployeeAddRequest(request.EmployeeId, request.StartDate, request.EndDate),
            dbContext,
            cancellationToken);

        return TypedResults.Ok(impact);
    }
}

