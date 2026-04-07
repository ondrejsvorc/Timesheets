using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Employees.Endpoints;

public sealed class GetEmployee : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}", Handle)
           .WithSummary("Get Employee");

    public sealed record Response(Guid Id, Guid? EmployeeTypeId, string FullName, int PersonalNumber, string Email);

    private static async Task<Results<Ok<Response>, NotFound>> Handle(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Response? employee = await dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new Response(
                e.Id,
                e.EmployeeTypeId,
                e.FullName,
                e.PersonalNumber,
                e.Email
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (employee is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(employee);
    }
}
