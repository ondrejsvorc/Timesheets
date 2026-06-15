using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Auth;
using Timesheets.Api.Common;
using Timesheets.Api.Data;

namespace Timesheets.Api.Contracts.Endpoints;

public sealed class GetContractEmployees : IEndpoint
{
    private static readonly TimeZoneInfo CzechTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}/employees", Handle)
           .WithSummary("Get Contract Employees");

    public sealed record PositionItem(Guid Id, string PositionCode, string Position, decimal Workload, DateTime StartDate, DateTime? EndDate, bool IsActive);
    public sealed record EmployeeItem(Guid Id, string PersonalNumber, string FullName, string EmployeeType, IReadOnlyList<PositionItem> Positions);
    public sealed record Response(DateTime ProjectStartDate, DateTime? ProjectEndDate, IEnumerable<EmployeeItem> Employees);

    private static async Task<Results<Ok<Response>, NotFound, ForbidHttpResult>> Handle(Guid id, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.Satisfies(UserRole.Employee, contractId: id))
        {
            return TypedResults.Forbid();
        }

        var projectRange = await dbContext.Contracts
            .AsNoTracking()
            .Where(contract => contract.Id == id)
            .Select(contract => new { contract.Project.StartDate, contract.Project.EndDate })
            .SingleOrDefaultAsync(cancellationToken);

        if (projectRange is null)
        {
            return TypedResults.NotFound();
        }

        DateTime localToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CzechTimeZone).Date;
        DateTime today = new(localToday.Year, localToday.Month, localToday.Day, 0, 0, 0, DateTimeKind.Utc);
        List<EmployeeItem> employees = await dbContext.ContractEmployees
            .AsNoTracking()
            .Where(ce => ce.ContractId == id)
            .Include(ce => ce.Employee)
                .ThenInclude(e => e.EmployeeType)
            .GroupBy(ce => ce.Employee)
            .Select(g => new EmployeeItem(g.Key.Id, g.Key.PersonalNumber, EmployeeNameFormatter.Format(g.Key.TitleBefore, g.Key.FullName, g.Key.TitleAfter), g.Key.EmployeeTypeId != null ? g.Key.EmployeeType.Name : string.Empty, g.Select(ce => new PositionItem(ce.Id, ce.PositionCode, ce.Position, ce.Workload, ce.StartDate, ce.EndDate, ce.EndDate == null || ce.EndDate >= today)).ToList()
            ))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(projectRange.StartDate, projectRange.EndDate, employees));
    }
}
