using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
using Timesheets.Api.Auth;
using Timesheets.Api.Common;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Employees.Endpoints;

public sealed class GetEmployees : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/", Handle)
           .WithSummary("Get Employees");

    public sealed record EmployeeItem(Guid Id, Guid? EmployeeTypeId, string PersonalNumber, string FullName, string? Email, bool IsGlobalManager);
    public sealed record Response(IEnumerable<EmployeeItem> Employees);

    private static async Task<Results<Ok<Response>, ForbidHttpResult>> Handle(
        HttpContext httpContext,
        AppDbContext dbContext,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        (_, UserPermissionsScope scope) = await PermissionsScopeResolver.ResolveRequiredAsync(
            httpContext, dbContext, administrationOptions, cancellationToken);

        if (!scope.CanListEmployees)
        {
            return TypedResults.Forbid();
        }

        IQueryable<Employee> query = dbContext.Employees.AsNoTracking();

        if (!scope.HasGlobalScope)
        {
            HashSet<Guid> visibleContractIds = scope.VisibleContractIds.ToHashSet();
            HashSet<Guid> visibleProjectIds = scope.VisibleProjectIds.ToHashSet();

            query = query.Where(e =>
                dbContext.ContractEmployees.Any(ce =>
                    ce.EmployeeId == e.Id
                    && (visibleContractIds.Contains(ce.ContractId)
                        || dbContext.Contracts.Any(c => c.Id == ce.ContractId && visibleProjectIds.Contains(c.ProjectId)))));
        }

        List<EmployeeItem> employees = await query
            .Select(e => new EmployeeItem(
                e.Id,
                e.EmployeeTypeId,
                e.PersonalNumber,
                EmployeeNameFormatter.Format(e.TitleBefore, e.FullName, e.TitleAfter),
                e.Email,
                e.IsGlobalManager
            ))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(employees));
    }
}
