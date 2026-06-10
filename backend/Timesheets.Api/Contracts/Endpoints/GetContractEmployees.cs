using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
using Timesheets.Api.Auth;
using Timesheets.Api.Common;
using Timesheets.Api.Data;

namespace Timesheets.Api.Contracts.Endpoints;

public sealed class GetContractEmployees : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}/employees", Handle)
           .WithSummary("Get Contract Employees");

    public sealed record PositionItem(Guid Id, string PositionCode, string Position, decimal Workload, DateTime StartDate, DateTime? EndDate);
    public sealed record EmployeeItem(Guid Id, string PersonalNumber, string FullName, string EmployeeType, IReadOnlyList<PositionItem> Positions);
    public sealed record Response(IEnumerable<EmployeeItem> Employees);

    private static async Task<Results<Ok<Response>, NotFound, ForbidHttpResult>> Handle(
        Guid id,
        HttpContext httpContext,
        AppDbContext dbContext,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        (_, UserPermissionsScope scope) = await PermissionsScopeResolver.ResolveRequiredAsync(
            httpContext, dbContext, administrationOptions, cancellationToken);

        if (!ApiPermissions.CanAccessContract(scope, id))
        {
            return TypedResults.Forbid();
        }

        bool contractExists = await dbContext.Contracts
            .AsNoTracking()
            .AnyAsync(contract => contract.Id == id, cancellationToken);

        if (!contractExists)
        {
            return TypedResults.NotFound();
        }

        List<EmployeeItem> employees = await dbContext.ContractEmployees
            .AsNoTracking()
            .Where(ce => ce.ContractId == id)
            .Include(ce => ce.Employee)
                .ThenInclude(e => e.EmployeeType)
            .GroupBy(ce => ce.Employee)
            .Select(g => new EmployeeItem(
                g.Key.Id,
                g.Key.PersonalNumber,
                EmployeeNameFormatter.Format(g.Key.TitleBefore, g.Key.FullName, g.Key.TitleAfter),
                g.Key.EmployeeTypeId != null ? g.Key.EmployeeType.Name : string.Empty,
                g.Select(ce => new PositionItem(ce.Id, ce.PositionCode, ce.Position, ce.Workload, ce.StartDate, ce.EndDate)).ToList()
            ))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(employees));
    }
}
