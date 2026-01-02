using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class GetProjectContractsManagers : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}/contracts/managers", Handle)
           .WithSummary("Get Project Contracts Managers");

    public sealed record ContractManagerItem(Guid ContractId, Guid EmployeeId, string ContractName, int EmployeePersonalNumber, string EmployeeFullName, string EmployeeEmail);
    public sealed record Response(IEnumerable<ContractManagerItem> Managers);

    private static async Task<Ok<Response>> Handle(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        List<ContractManagerItem> managers = await dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.ProjectId == id)
            .SelectMany(
                c => c.ContractManagers,
                (contract, manager) => new ContractManagerItem(
                    contract.Id,
                    manager.Employee.Id,
                    contract.Name,
                    manager.Employee.PersonalNumber,
                    manager.Employee.FullName,
                    manager.Employee.Email
                )
            )
            .OrderBy(m => m.ContractName)
            .ThenBy(m => m.EmployeeFullName)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(managers));
    }
}

