using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
using Timesheets.Api.Auth;
using Timesheets.Api.Data;

namespace Timesheets.Api.Employees.Endpoints;

public sealed class GetEmployeePositions : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}/positions", Handle)
           .WithSummary("Get Employee Positions");

    public sealed record EmployeePositionItem(
        Guid ProjectId,
        string ProjectName,
        Guid ContractId,
        string ContractRegistrationNumber,
        string PositionCode,
        string Position,
        decimal Workload,
        DateTime StartDate,
        DateTime? EndDate
    );
    public sealed record Response(Guid EmployeeId, IEnumerable<EmployeePositionItem> Positions);

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

        bool employeeExists = await dbContext.Employees
            .AsNoTracking()
            .AnyAsync(e => e.Id == id, cancellationToken);

        if (!employeeExists)
        {
            return TypedResults.NotFound();
        }

        List<EmployeePositionItem> positions = await dbContext.ContractEmployees
            .AsNoTracking()
            .Where(e => e.EmployeeId == id)
            .OrderBy(e => e.Contract.Project.Name)
            .ThenBy(e => e.Contract.RegistrationNumber)
            .ThenBy(e => e.StartDate)
            .Select(e => new EmployeePositionItem(
                e.Contract.Project.Id,
                e.Contract.Project.Name,
                e.Contract.Id,
                e.Contract.RegistrationNumber,
                e.PositionCode,
                e.Position,
                e.Workload,
                e.StartDate,
                e.EndDate
            ))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(id, positions));
    }
}
