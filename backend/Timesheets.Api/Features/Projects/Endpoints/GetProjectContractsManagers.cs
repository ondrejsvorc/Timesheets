using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Projects.Endpoints;

public sealed class GetProjectContractsManagers : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}/contracts/managers", Handle)
           .WithSummary("Get Project Contracts Managers");

    public sealed record ContractManagerItem(
        Guid ContractId,
        Guid EmployeeId,
        string ContractRegistrationNumber,
        string EmployeePersonalNumber,
        string EmployeeFullName);
    public sealed record Response(IEnumerable<ContractManagerItem> Managers);

    private static async Task<Results<Ok<Response>, ForbidHttpResult>> Handle(Guid id, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.Satisfies(UserRole.ProjectManager, projectId: id))
        {
            return TypedResults.Forbid();
        }

        List<ContractManagerItem> managers = (await dbContext.ContractManagers
            .AsNoTracking()
            .Where(cm => cm.Contract.ProjectId == id)
            .Include(cm => cm.Employee)
            .Include(cm => cm.Contract)
            .OrderBy(cm => cm.Contract.RegistrationNumber)
            .ThenBy(cm => cm.Employee.Surname)
            .ThenBy(cm => cm.Employee.FirstName)
            .ToListAsync(cancellationToken))
            .Select(cm => new ContractManagerItem(
                cm.ContractId,
                cm.EmployeeId,
                cm.Contract.RegistrationNumber,
                cm.Employee.PersonalNumber,
                cm.Employee.DisplayName
            ))
            .ToList();

        return TypedResults.Ok(new Response(managers));
    }
}
