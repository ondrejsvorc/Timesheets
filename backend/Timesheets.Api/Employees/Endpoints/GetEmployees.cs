using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Employees.Endpoints;

public sealed class GetEmployees : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/", Handle)
           .WithSummary("Get Employees");

    public sealed record EmployeeItem(Guid Id, Guid? EmployeeTypeId, int PersonalNumber, string FullName, string? Email, bool IsGlobalManager);
    public sealed record Response(IEnumerable<EmployeeItem> Employees);

    private static async Task<Ok<Response>> Handle(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        List<EmployeeItem> employees = await dbContext.Employees
            .AsNoTracking()
            .Select(e => new EmployeeItem(
                e.Id,
                e.EmployeeTypeId,
                e.PersonalNumber,
                e.FullName,
                e.Email,
                e.IsGlobalManager
            ))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(employees));
    }
}