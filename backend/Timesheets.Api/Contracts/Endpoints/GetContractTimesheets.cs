using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Contracts.Endpoints;

public sealed class GetContractTimesheets : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}/timesheets", Handle)
           .WithSummary("Get Contract Timesheets")
           .WithRequestValidation<Request>();

    public sealed record TimesheetItem(
        Guid Id,
        Guid EmployeeId,
        int Year,
        int Month,
        string? PositionCode,
        string Position,
        decimal Workload,
        Guid StatusId,
        string Status
    );

    public sealed record EmployeeItem(
        Guid Id,
        int PersonalNumber,
        string FullName,
        string EmployeeType
    );

    public sealed record Request(
        [FromQuery(Name = "fromYear")] int FromYear,
        [FromQuery(Name = "fromMonth")] int FromMonth,
        [FromQuery(Name = "toYear")] int ToYear,
        [FromQuery(Name = "toMonth")] int ToMonth,
        [FromQuery(Name = "status")] string[] Statuses
    );

    public sealed record Response(
        IEnumerable<EmployeeItem> Employees,
        IEnumerable<TimesheetItem> Timesheets
    );

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.FromMonth).InclusiveBetween(1, 12);
            RuleFor(x => x.ToMonth).InclusiveBetween(1, 12);
            RuleFor(x => x).Must(x => x.FromYear < x.ToYear || (x.FromYear == x.ToYear && x.FromMonth <= x.ToMonth))
                .WithMessage("The From (year, month) must not be greater than the To (year, month).");
        }
    }

    private static async Task<Results<Ok<Response>, NotFound>> Handle(Guid id, [AsParameters] Request request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        bool contractExists = await dbContext.Contracts
            .AsNoTracking()
            .AnyAsync(contract => contract.Id == id, cancellationToken);

        if (!contractExists)
        {
            return TypedResults.NotFound();
        }

        IQueryable<AttendanceTimesheet> query = dbContext.AttendanceTimesheets
            .AsNoTracking()
            .Where(timesheet => timesheet.Year > request.FromYear || (timesheet.Year == request.FromYear && timesheet.Month >= request.FromMonth))
            .Where(timesheet => timesheet.Year < request.ToYear || (timesheet.Year == request.ToYear && timesheet.Month <= request.ToMonth));

        if (request.Statuses.Length != 0)
        {
            query = query.Where(timesheet => request.Statuses.Contains(timesheet.TimesheetStatus.Name));
        }

        var items = await query
            .Select(timesheet => new
            {
                timesheet.Id,
                timesheet.EmployeeId,
                timesheet.Year,
                timesheet.Month,
                timesheet.TimesheetStatusId,
                Status = timesheet.TimesheetStatus.Name,
                timesheet.Employee.PersonalNumber,
                timesheet.Employee.FullName,
                EmployeeType = timesheet.Employee.EmployeeType.Name,
                ContractEmployee = dbContext.ContractEmployees
                    .Where(employee => employee.ContractId == id)
                    .Where(employee => employee.EmployeeId == timesheet.EmployeeId)
                    .Where(employee =>
                        employee.StartDate <= new DateTime(timesheet.Year, timesheet.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1).AddDays(-1)
                        && (employee.EndDate == null || employee.EndDate >= new DateTime(timesheet.Year, timesheet.Month, 1, 0, 0, 0, DateTimeKind.Utc)))
                    .OrderByDescending(employee => employee.StartDate)
                    .Select(employee => new { employee.PositionCode, employee.Position, employee.Workload })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return TypedResults.Ok(new Response(Employees: [], Timesheets: []));
        }

        List<TimesheetItem> timesheets = items
            .Where(item => item.ContractEmployee is not null)
            .Select(item => new TimesheetItem(
                item.Id,
                item.EmployeeId,
                item.Year,
                item.Month,
                item.ContractEmployee!.PositionCode,
                item.ContractEmployee!.Position,
                item.ContractEmployee!.Workload,
                item.TimesheetStatusId,
                item.Status
            ))
            .ToList();

        List<EmployeeItem> employees = items
            .Select(item => new EmployeeItem(
                item.EmployeeId,
                item.PersonalNumber,
                item.FullName,
                item.EmployeeType
            ))
            .Distinct()
            .ToList();

        return TypedResults.Ok(new Response(employees, timesheets));
    }
}
