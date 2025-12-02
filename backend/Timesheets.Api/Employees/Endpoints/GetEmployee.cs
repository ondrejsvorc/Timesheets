using Microsoft.AspNetCore.Http.HttpResults;
using Timesheets.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Timesheets.Api.Employees.Endpoints;

public sealed class GetEmployee : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}", Handle)
           .WithSummary("Get Employee");

    public sealed record Response(Guid Id, Guid EmployeeTypeId, int PersonalNumber, string FullName, string? Email, bool IsGlobalManager);

    private static async Task<Results<Ok<Response>, NotFound>> Handle(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Response? employee = await dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new Response(
                e.Id,
                e.EmployeeTypeId,
                e.PersonalNumber,
                e.FullName,
                e.Email,
                e.IsGlobalManager
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (employee is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(employee);
    }
}