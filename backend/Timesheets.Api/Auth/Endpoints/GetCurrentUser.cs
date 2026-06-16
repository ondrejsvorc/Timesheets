using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Auth.Endpoints;

public sealed class GetCurrentUser : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/currentUser", Handle)
           .WithSummary("Get Currently Authenticated User");

    public sealed record Response(
        Guid Id,
        string FullName,
        string? EmployeeType,
        string PersonalNumber,
        string? TitleBefore,
        string? TitleAfter
    );

    private static async Task<IResult> Handle(HttpContext httpContext, AppDbContext dbContext, CancellationToken cancellationToken)
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

        Response response = new(
            Id: employee.Id,
            FullName: EmployeeNameFormatter.Format(employee.TitleBefore, employee.FullName, employee.TitleAfter),
            EmployeeType: employee.EmployeeTypeId == null ? null : employee.EmployeeType?.Name,
            PersonalNumber: employee.PersonalNumber,
            TitleBefore: employee.TitleBefore,
            TitleAfter: employee.TitleAfter);

        return Results.Ok(response);
    }
}
