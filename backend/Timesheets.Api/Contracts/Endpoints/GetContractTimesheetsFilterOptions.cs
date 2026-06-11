using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Auth;
using Timesheets.Api.Data;

namespace Timesheets.Api.Contracts.Endpoints;

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

    private static async Task<Results<Ok<Response>, NotFound, ForbidHttpResult>> Handle(
        Guid id,
        AppDbContext dbContext,
        ICurrentUser user,
        CancellationToken cancellationToken)
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

        var baseQuery = dbContext.AttendanceTimesheets
            .AsNoTracking()
            .SelectMany(
                timesheet => dbContext.ContractEmployees
                    .AsNoTracking()
                    .Where(contractEmployee => contractEmployee.ContractId == id)
                    .Where(contractEmployee => contractEmployee.EmployeeId == timesheet.EmployeeId)
                    .Where(contractEmployee =>
                        contractEmployee.StartDate <= new DateTime(timesheet.Year, timesheet.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1).AddDays(-1)
                        && (contractEmployee.EndDate == null || contractEmployee.EndDate >= new DateTime(timesheet.Year, timesheet.Month, 1, 0, 0, 0, DateTimeKind.Utc))
                    ),
                (timesheet, _) => new { timesheet.Year, timesheet.Month, Status = timesheet.TimesheetStatus.Name }
            );

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

