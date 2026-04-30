using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;

namespace Timesheets.Api.Auth.Endpoints;

public sealed class GetCurrentUser : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/currentUser", Handle)
           .WithSummary("Get Currently Authenticated User");

    public sealed record Response(
        Guid Id,
        string FullName,
        string Email,
        string EmployeeType,
        string PersonalNumber,
        string? TitleBefore,
        string? TitleAfter
    );

    private static async Task<IResult> Handle(HttpContext httpContext, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        ClaimsPrincipal currentUser = httpContext.User;
        if (!currentUser.IsAuthenticated())
        {
            return Results.Unauthorized();
        }

        string email = currentUser.GetEmail();
        Response? response = await dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Email == email)
            .Select(e => new Response(
                Id: e.Id,
                FullName: e.FullName,
                Email: e.Email,
                EmployeeType: e.EmployeeType.Name,
                PersonalNumber: e.PersonalNumber,
                TitleBefore: e.TitleBefore,
                TitleAfter: e.TitleAfter
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return Results.NotFound("Employee not found.");
        }

        return Results.Ok(response);
    }
}

