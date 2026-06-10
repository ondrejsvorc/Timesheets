using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
using Timesheets.Api.Auth;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;

namespace Timesheets.Api.Contracts.Endpoints;

public sealed class RemoveContractEmployee : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/{id}/employees/{contractEmployeeId}", Handle)
           .WithSummary("Remove Employee Position from Contract");

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> Handle(
        Guid id,
        Guid contractEmployeeId,
        HttpContext httpContext,
        AppDbContext dbContext,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        (_, UserPermissionsScope scope) = await PermissionsScopeResolver.ResolveRequiredAsync(
            httpContext, dbContext, administrationOptions, cancellationToken);

        if (!await ApiPermissions.CanManageContractEmployeesAsync(scope, id, dbContext, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var ce = await dbContext.ContractEmployees
            .AsNoTracking()
            .Where(x => x.ContractId == id && x.Id == contractEmployeeId)
            .Select(x => new { x.EmployeeId, x.StartDate, x.EndDate })
            .FirstOrDefaultAsync(cancellationToken);

        if (ce is null)
        {
            return TypedResults.NotFound();
        }

        // delete project timesheets for this contract employee in months overlapped by assignment
        DateTime start = ce.StartDate.Kind == DateTimeKind.Utc ? ce.StartDate : DateTime.SpecifyKind(ce.StartDate, DateTimeKind.Utc);
        DateTime end = ce.EndDate.HasValue
            ? (ce.EndDate.Value.Kind == DateTimeKind.Utc ? ce.EndDate.Value : DateTime.SpecifyKind(ce.EndDate.Value, DateTimeKind.Utc))
            : ce.StartDate;
        DateTime cursor = new(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime last = new(end.Year, end.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        while (cursor <= last)
        {
            int year = cursor.Year;
            int month = cursor.Month;
            await dbContext.ProjectTimesheets
                .Where(t => t.ContractEmployeeId == contractEmployeeId && t.Year == year && t.Month == month)
                .ExecuteDeleteAsync(cancellationToken);
            cursor = cursor.AddMonths(1);
        }

        int affected = await dbContext.ContractEmployees
            .Where(x => x.ContractId == id && x.Id == contractEmployeeId)
            .ExecuteDeleteAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
