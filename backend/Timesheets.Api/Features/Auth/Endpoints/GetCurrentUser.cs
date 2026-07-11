using Microsoft.Extensions.Options;
using Timesheets.Api.Common;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Features.Auth.Endpoints;

public sealed class GetCurrentUser : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/currentUser", Handle)
           .WithSummary("Get Currently Authenticated User");

    public sealed record PermissionsResponse(
        UserRole Role,
        IReadOnlyList<Guid> ProjectManagerOf,
        IReadOnlyList<Guid> ContractManagerOf,
        IReadOnlyList<Guid> EmployeeOnContractIds,
        IReadOnlyList<Guid> VisibleProjectIds,
        IReadOnlyList<Guid> VisibleContractIds);

    public sealed record Response(
        Guid Id,
        string FullName,
        string? EmployeeType,
        string PersonalNumber,
        string? TitleBefore,
        string? TitleAfter,
        PermissionsResponse Permissions);

    private static async Task<IResult> Handle(
        HttpContext httpContext,
        AppDbContext dbContext,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthenticated())
        {
            return Results.Unauthorized();
        }

        Employee? employee = await CurrentEmployeeResolver.TryGetAsync(httpContext.User, dbContext, cancellationToken);
        if (employee is null)
        {
            return Results.NotFound("Employee not found.");
        }

        UserPermissions permissions = await UserPermissionsLoader.LoadAsync(employee, dbContext, administrationOptions, cancellationToken);

        Response response = new(
            Id: employee.Id,
            FullName: employee.DisplayName,
            EmployeeType: employee.EmployeeType.Name,
            PersonalNumber: employee.PersonalNumber,
            TitleBefore: employee.TitleBefore,
            TitleAfter: employee.TitleAfter,
            Permissions: new PermissionsResponse(
                Role: permissions.Role,
                ProjectManagerOf: permissions.ProjectManagerOf,
                ContractManagerOf: permissions.ContractManagerOf,
                EmployeeOnContractIds: permissions.EmployeeOnContractIds,
                VisibleProjectIds: permissions.VisibleProjectIds,
                VisibleContractIds: permissions.VisibleContractIds));

        return Results.Ok(response);
    }
}
