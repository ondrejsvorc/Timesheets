using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Timesheets.Api.Data;

namespace Timesheets.Api.Auth.Endpoints;

public sealed class GetCurrentUser : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/currentUser", Handle)
           .WithSummary("Get Currently Authenticated User");

    public sealed record Response(Guid Id, string FullName, string Email, string EmployeeType);

    private static async Task<IResult> Handle(HttpContext httpContext, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        ClaimsPrincipal principal = httpContext.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        string email = principal.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? "";

        Response? response = await dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Email == email)
            .Select(e => new Response(
                Id: e.Id,
                FullName: e.FullName,
                Email: e.Email,
                EmployeeType: e.EmployeeType.Name
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return Results.NotFound("Employee not found.");
        }

        return Results.Ok(response);
    }
}

