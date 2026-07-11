using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Contracts.Endpoints;

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
        string PersonalNumber,
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

    private static async Task<Results<Ok<Response>, NotFound, ForbidHttpResult>> Handle(Guid id, [AsParameters] Request request, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
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

        bool canViewAllTimesheets = await user.CanViewAllContractTimesheetsAsync(id, cancellationToken);

        IQueryable<ProjectTimesheet> query = dbContext.ProjectTimesheets
            .AsNoTracking()
            .Where(timesheet => timesheet.ContractId == id)
            .Where(timesheet => timesheet.Year > request.FromYear || (timesheet.Year == request.FromYear && timesheet.Month >= request.FromMonth))
            .Where(timesheet => timesheet.Year < request.ToYear || (timesheet.Year == request.ToYear && timesheet.Month <= request.ToMonth))
            .Where(timesheet => dbContext.AttendanceTimesheets.Any(attendance =>
                attendance.EmployeeId == timesheet.EmployeeId
                && attendance.Year == timesheet.Year
                && attendance.Month == timesheet.Month));

        if (!canViewAllTimesheets)
        {
            query = query.Where(timesheet => timesheet.EmployeeId == user.EmployeeId);
        }

        if (request.Statuses.Length != 0)
        {
            query = query.Where(timesheet => request.Statuses.Contains(timesheet.TimesheetStatus.Name));
        }

        var items = await query
            .Join(
                dbContext.ContractEmployees.AsNoTracking(),
                timesheet => timesheet.ContractEmployeeId,
                contractEmployee => contractEmployee.Id,
                (timesheet, contractEmployee) => new { timesheet, contractEmployee })
            .Join(
                dbContext.Employees.AsNoTracking(),
                x => x.timesheet.EmployeeId,
                employee => employee.Id,
                (x, employee) => new
                {
                    x.timesheet.Id,
                    x.timesheet.EmployeeId,
                    x.timesheet.Year,
                    x.timesheet.Month,
                    x.timesheet.TimesheetStatusId,
                    Status = x.timesheet.TimesheetStatus.Name,
                    x.timesheet.Workload,
                    x.contractEmployee.PositionCode,
                    x.contractEmployee.Position,
                    Employee = employee,
                    EmployeeType = employee.EmployeeType.Name,
                })
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return TypedResults.Ok(new Response(Employees: [], Timesheets: []));
        }

        List<TimesheetItem> timesheets = items
            .Select(item => new TimesheetItem(
                item.Id,
                item.EmployeeId,
                item.Year,
                item.Month,
                item.PositionCode,
                item.Position,
                item.Workload,
                item.TimesheetStatusId,
                item.Status
            ))
            .ToList();

        List<EmployeeItem> employees = items
            .Select(item => new EmployeeItem(
                item.EmployeeId,
                item.Employee.PersonalNumber,
                item.Employee.DisplayName,
                item.EmployeeType
            ))
            .DistinctBy(item => item.Id)
            .ToList();

        return TypedResults.Ok(new Response(employees, timesheets));
    }
}
