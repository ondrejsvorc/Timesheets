using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common;
using Timesheets.Api.Data;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Employees.Endpoints;

public sealed class GetEmployee : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}", Handle)
           .WithSummary("Get Employee");

    public sealed record Response(Guid Id, Guid? EmployeeTypeId, string FullName, string PersonalNumber);

    private static async Task<Results<Ok<Response>, NotFound, ForbidHttpResult>> Handle(Guid id, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!await user.CanAccessEmployeeAsync(id, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        Response? employee = await dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new Response(
                e.Id,
                e.EmployeeTypeId,
                EmployeeNameFormatter.Format(e.TitleBefore, e.FullName, e.TitleAfter),
                e.PersonalNumber
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (employee is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(employee);
    }
}
