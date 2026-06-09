using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
using Timesheets.Api.Auth;
using Timesheets.Api.Common;
using Timesheets.Api.Data;

namespace Timesheets.Api.Employees.Endpoints;

public sealed class GetEmployee : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}", Handle)
           .WithSummary("Get Employee");

    public sealed record Response(Guid Id, Guid? EmployeeTypeId, string FullName, string PersonalNumber, string Email);

    private static async Task<Results<Ok<Response>, NotFound, ForbidHttpResult>> Handle(
        Guid id,
        HttpContext httpContext,
        AppDbContext dbContext,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        (_, UserPermissionsScope scope) = await PermissionsScopeResolver.ResolveRequiredAsync(
            httpContext, dbContext, administrationOptions, cancellationToken);

        if (!await ApiPermissions.CanAccessEmployeeAsync(scope, id, dbContext, cancellationToken))
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
