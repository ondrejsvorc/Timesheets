using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Contracts.Endpoints;

public sealed class GetContractEmployees : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}/employees", Handle)
           .WithSummary("Get Contract Employees");

    public sealed record PositionItem(string? Position, decimal? Workload);
    public sealed record EmployeeItem(Guid Id, int? PersonalNumber, string FullName, string EmployeeType, IReadOnlyList<PositionItem> Positions);
    public sealed record Response(IEnumerable<EmployeeItem> Employees);

    private static async Task<Results<Ok<Response>, NotFound>> Handle(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        bool contractExists = await dbContext.Contracts
            .AsNoTracking()
            .AnyAsync(contract => contract.Id == id, cancellationToken);

        if (!contractExists)
        {
            return TypedResults.NotFound();
        }

        List<EmployeeItem> employees = await dbContext.ContractEmployees
            .AsNoTracking()
            .Where(ce => ce.ContractId == id)
            .Include(ce => ce.Employee)
                .ThenInclude(e => e.EmployeeType)
            .GroupBy(ce => ce.Employee)
            .Select(g => new EmployeeItem(
                g.Key.Id,
                g.Key.PersonalNumber,
                g.Key.FullName,
                g.Key.EmployeeType.Name,
                g.Select(ce => new PositionItem(ce.Position, ce.Workload)).ToList()
            ))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(employees));
    }
}

