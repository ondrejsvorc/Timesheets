using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Auth;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;

namespace Timesheets.Api.Employees.Endpoints;

public sealed class UpdateEmployeeType : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPatch("/{id}/type", Handle)
           .WithSummary("Update Employee Type")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request(Guid? EmployeeTypeId);

    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Results<NoContent, NotFound, BadRequest<string>, ForbidHttpResult>> Handle(
        Guid id,
        [FromBody] Request request,
        AppDbContext dbContext,
        ICurrentUser user,
        CancellationToken cancellationToken)
    {
        if (!user.IsGlobalManagerRole())
        {
            return TypedResults.Forbid();
        }

        if (request.EmployeeTypeId is Guid employeeTypeId)
        {
            bool exists = await dbContext.EmployeeTypes.AnyAsync(t => t.Id == employeeTypeId, cancellationToken);
            if (!exists)
            {
                return TypedResults.BadRequest("Employee type does not exist.");
            }
        }

        int affected = await dbContext.Employees
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.EmployeeTypeId, request.EmployeeTypeId)
                .SetProperty(e => e.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        if (affected == 0)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }
}
