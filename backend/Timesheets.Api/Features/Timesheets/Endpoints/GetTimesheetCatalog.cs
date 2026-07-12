using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;

namespace Timesheets.Api.Features.Timesheets.Endpoints;

public sealed class GetTimesheetCatalog : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/catalog", Handle)
           .WithSummary("Get Timesheet Catalog");

    public sealed record Request([FromQuery] Guid EmployeeId, [FromQuery] int Year, [FromQuery] int Month);
    public sealed record ContractPartItem(Guid Id, string Label);
    public sealed record Response(Guid TimesheetId, Guid CurrentStatusId, IEnumerable<ContractPartItem> ContractParts);
    private sealed record ContractPartRow(Guid Id, string ContractRegistrationNumber);

    private static async Task<Results<Ok<Response>, NotFound>> Handle([AsParameters] Request request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var attendanceTimesheet = await dbContext.Timesheets
            .AsNoTracking()
            .Where(t => t.EmployeeId == request.EmployeeId && t.Year == request.Year && t.Month == request.Month)
            .Select(t => new { t.Id, t.TimesheetStatusId })
            .SingleOrDefaultAsync(cancellationToken);

        if (attendanceTimesheet is null)
        {
            return TypedResults.NotFound();
        }

        List<ContractPartRow> contractPartRows = await dbContext.ContractParts
            .AsNoTracking()
            .Where(part => part.TimesheetId == attendanceTimesheet.Id)
            .Join(dbContext.ContractEmployees.AsNoTracking(), part => part.ContractEmployeeId, contractEmployee => contractEmployee.Id, (part, contractEmployee) => new { part, contractEmployee })
            .Join(dbContext.Contracts.AsNoTracking(), x => x.contractEmployee.ContractId, contract => contract.Id, (x, contract) => new { x.part, contract })
            .OrderBy(x => x.contract.RegistrationNumber)
            .Select(x => new ContractPartRow(x.part.Id, x.contract.RegistrationNumber))
            .ToListAsync(cancellationToken);

        List<ContractPartItem> contractParts = contractPartRows.Select(row => new ContractPartItem(row.Id, row.ContractRegistrationNumber)).ToList();

        return TypedResults.Ok(new Response(attendanceTimesheet.Id, attendanceTimesheet.TimesheetStatusId, contractParts));
    }
}
