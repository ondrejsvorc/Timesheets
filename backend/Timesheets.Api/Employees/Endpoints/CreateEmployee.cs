using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
using Timesheets.Api.Auth;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Employees.Endpoints;

public sealed class CreateEmployee : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/", Handle)
           .WithSummary("Create Employee")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request(Guid EmployeeTypeId, string PersonalNumber, string FullName, string Email);
    public sealed record Response(Guid Id);
    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Results<Created<Response>, BadRequest<string>, ForbidHttpResult>> Handle(
        [FromBody] Request request,
        HttpContext httpContext,
        AppDbContext dbContext,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        (_, UserPermissionsScope scope) = await PermissionsScopeResolver.ResolveRequiredAsync(
            httpContext, dbContext, administrationOptions, cancellationToken);

        if (!ApiPermissions.CanModifyProjects(scope))
        {
            return TypedResults.Forbid();
        }

        Employee employee = new()
        {
            Id = Guid.NewGuid(),
            EmployeeTypeId = request.EmployeeTypeId,
            PersonalNumber = request.PersonalNumber,
            FullName = request.FullName,
            Email = request.Email,
            IsGlobalManager = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/employees/{employee.Id}", new Response(employee.Id));
    }
}
