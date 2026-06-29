using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Auth;
using Timesheets.Api.Data;

namespace Timesheets.Api.Employees.Endpoints;

public sealed class GetEmployeePositions : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}/positions", Handle)
           .WithSummary("Get Employee Positions");

    public sealed record EmployeePositionItem(
        Guid Id,
        Guid ProjectId,
        string ProjectName,
        DateTime ProjectStartDate,
        DateTime? ProjectEndDate,
        Guid ContractId,
        string ContractRegistrationNumber,
        string PositionCode,
        string Position,
        decimal Workload,
        DateTime StartDate,
        DateTime? EndDate
    );
    public sealed record Response(Guid EmployeeId, IEnumerable<EmployeePositionItem> Positions);

    private static async Task<Results<Ok<Response>, NotFound, ForbidHttpResult>> Handle(Guid id, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!await user.CanAccessEmployeeAsync(id, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        bool employeeExists = await dbContext.Employees
            .AsNoTracking()
            .AnyAsync(e => e.Id == id, cancellationToken);

        if (!employeeExists)
        {
            return TypedResults.NotFound();
        }

        List<EmployeePositionItem> positions = await dbContext.ContractEmployees
            .AsNoTracking()
            .Where(e => e.EmployeeId == id)
            .OrderBy(e => e.ContractPosition.Contract.Project.Name)
            .ThenBy(e => e.ContractPosition.Contract.RegistrationNumber)
            .ThenBy(e => e.StartDate)
            .Select(e => new EmployeePositionItem(
                e.Id,
                e.ContractPosition.Contract.Project.Id,
                e.ContractPosition.Contract.Project.Name,
                e.ContractPosition.Contract.Project.StartDate,
                e.ContractPosition.Contract.Project.EndDate,
                e.ContractPosition.Contract.Id,
                e.ContractPosition.Contract.RegistrationNumber,
                e.ContractPosition.Code,
                e.ContractPosition.Name,
                e.Workload,
                e.StartDate,
                e.EndDate
            ))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(id, positions));
    }
}
