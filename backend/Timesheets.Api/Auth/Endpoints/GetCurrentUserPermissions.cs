using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;

namespace Timesheets.Api.Auth.Endpoints;

public sealed class GetCurrentUserPermissions : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/currentUserPermissions", Handle)
           .WithSummary("Get Currently Authenticated User Permissions");

    public sealed record Response(bool IsGlobalManager, IReadOnlyList<Guid> ProjectManagerOf, IReadOnlyList<Guid> ContractManagerOf);

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
                IsGlobalManager: e.IsGlobalManager,
                ProjectManagerOf: e.ProjectManagers.Select(pm => pm.ProjectId).ToList(),
                ContractManagerOf: e.ContractManagers.Select(cm => cm.ContractId).ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return Results.NotFound("Employee not found.");
        }

        return Results.Ok(response);
    }
}