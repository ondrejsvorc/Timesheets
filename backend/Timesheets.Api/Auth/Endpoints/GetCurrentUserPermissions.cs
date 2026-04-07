using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

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

        Employee? employee = await dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Email == email)
            .FirstOrDefaultAsync(cancellationToken);

        if (employee is null)
        {
            return Results.NotFound("Employee not found.");
        }

        List<Guid> projectManagerOf = await dbContext.ProjectManagers
            .AsNoTracking()
            .Where(pm => pm.EmployeeId == employee.Id)
            .Select(pm => pm.ProjectId)
            .ToListAsync(cancellationToken);

        List<Guid> contractManagerOf = await dbContext.ContractManagers
            .AsNoTracking()
            .Where(cm => cm.EmployeeId == employee.Id)
            .Select(cm => cm.ContractId)
            .ToListAsync(cancellationToken);

        Response response = new Response(
            IsGlobalManager: employee.IsGlobalManager,
            ProjectManagerOf: projectManagerOf,
            ContractManagerOf: contractManagerOf
        );

        return Results.Ok(response);
    }
}
