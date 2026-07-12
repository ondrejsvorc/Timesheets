using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Contracts.Endpoints;

public sealed class GetContractTimesheetsFilterOptions : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}/timesheets/filter-options", Handle)
           .WithSummary("Get Contract Timesheets Filter Options");

    public sealed record Response(
        IReadOnlyList<int> Years,
        IReadOnlyList<int> Months,
        IReadOnlyList<string> Statuses
    );

    private static async Task<Results<Ok<Response>, NotFound, ForbidHttpResult>> Handle(Guid id, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.Satisfies(UserRole.Employee, contractId: id))
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

        var baseQuery = dbContext.ContractParts
            .AsNoTracking()
            .Where(part => part.ContractEmployee.ContractId == id)
            .Where(part => dbContext.Attendances.Any(attendance => attendance.TimesheetId == part.TimesheetId))
            .Select(part => new { part.Timesheet.Year, part.Timesheet.Month, Status = part.TimesheetStatus.Name });

        var rows = await baseQuery
            .Distinct()
            .ToListAsync(cancellationToken);

        var years = rows
            .Select(x => x.Year)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var months = rows
            .Select(x => x.Month)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var statuses = rows
            .Select(x => x.Status)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        return TypedResults.Ok(new Response(years, months, statuses));
    }
}

