using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Employees.Endpoints;

public sealed class GetEmployees : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/", Handle)
           .WithSummary("Get Employees");

    public sealed record EmployeeItem(Guid Id, Guid? EmployeeTypeId, string PersonalNumber, string FullName, bool IsGlobalManager);
    public sealed record Response(IEnumerable<EmployeeItem> Employees);

    private static async Task<Results<Ok<Response>, ForbidHttpResult>> Handle(AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.IsContractManager())
        {
            return TypedResults.Forbid();
        }

        IQueryable<Employee> query = dbContext.Employees.AsNoTracking();

        if (!user.IsGlobalManagerRole())
        {
            HashSet<Guid> visibleContractIds = user.VisibleContractIds.ToHashSet();
            HashSet<Guid> visibleProjectIds = user.VisibleProjectIds.ToHashSet();

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
                e.IsGlobalManager
            ))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(employees));
    }
}
