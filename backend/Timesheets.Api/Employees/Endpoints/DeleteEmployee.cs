using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Auth;
using Timesheets.Api.Data;

namespace Timesheets.Api.Employees.Endpoints;

public sealed class DeleteEmployee : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/{id}", Handle)
           .WithSummary("Delete Employee");

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> Handle(
        Guid id,
        AppDbContext dbContext,
        ICurrentUser user,
        CancellationToken cancellationToken)
    {
        if (!user.IsGlobalManagerRole())
        {
            return TypedResults.Forbid();
        }

        int affected = await dbContext.Employees
            .Where(e => e.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        if (affected == 0)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }
}
