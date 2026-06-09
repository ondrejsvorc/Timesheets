using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
using Timesheets.Api.Common;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Employees.Endpoints;

public sealed class UpdateEmployeeGlobalManagerRole : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPatch("/{id}/global-manager", Handle)
           .WithSummary("Update Employee Global Manager Role")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request(bool IsGlobalManager);

    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Results<NoContent, NotFound, BadRequest<string>, UnauthorizedHttpResult>> Handle(
        Guid id,
        [FromBody] Request request,
        HttpContext httpContext,
        AppDbContext dbContext,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        Employee changedBy = await CurrentEmployeeResolver.GetRequiredAsync(httpContext.User, dbContext, cancellationToken);
        if (!RoleManagerAuthorization.IsRoleManager(changedBy.Email, administrationOptions.Value))
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
