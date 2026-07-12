using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Domain;
using Timesheets.Api.Domain.Models;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Employees.Endpoints;

public sealed class UpdateEmployeeGlobalManagerRole : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPatch("/{id}/global-manager", Handle)
           .WithSummary("Update Employee Global Manager Role")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request(bool IsGlobalManager);

    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Results<NoContent, NotFound, BadRequest<string>, UnauthorizedHttpResult>> Handle(Guid id, [FromBody] Request request, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.IsAdmin())
        {
            return TypedResults.Unauthorized();
        }

        Employee? employee = await dbContext.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee is null)
        {
            return TypedResults.NotFound();
        }

        if (employee.IsGlobalManager == request.IsGlobalManager)
        {
            return TypedResults.NoContent();
        }

        employee.IsGlobalManager = request.IsGlobalManager;
        employee.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
