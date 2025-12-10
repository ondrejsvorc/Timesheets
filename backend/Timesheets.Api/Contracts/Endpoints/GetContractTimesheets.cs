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

    public enum GroupByOption { Employee, Month }

    public sealed record TimesheetItem(
        Guid Id,
        string? Position,
        decimal? Workload,
        Guid StatusId,
        string Status
    );

    public sealed record EmployeeGroup(
        Guid Id,
        bool AllTimesheetsApproved,
        int? PersonalNumber,
        string FullName,
        string EmployeeType,
        IEnumerable<TimesheetItem> Timesheets
    );

    public sealed record MonthGroup(
        int Year,
        int Month,
        IEnumerable<EmployeeGroup> Items
    );

    public sealed record Request(
        [FromQuery(Name = "fromYear")] int FromYear,
        [FromQuery(Name = "fromMonth")] int FromMonth,
        [FromQuery(Name = "toYear")] int ToYear,
        [FromQuery(Name = "toMonth")] int ToMonth,
        [FromQuery(Name = "groupBy")] GroupByOption GroupBy,
        [FromQuery(Name = "status")] string[]? Statuses
    );

    public sealed record Response(
        IEnumerable<EmployeeGroup> Employees,
        IEnumerable<MonthGroup> Months
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
            .Where(timesheet => timesheet.ContractId == id)
            .Where(timesheet => timesheet.Year > request.FromYear || (timesheet.Year == request.FromYear && timesheet.Month >= request.FromMonth))
            .Where(timesheet => timesheet.Year < request.ToYear || (timesheet.Year == request.ToYear && timesheet.Month <= request.ToMonth));

        if (request.Statuses?.Any() == true)
        {
            query = query.Where(t => request.Statuses.Contains(t.TimesheetStatus.Name));
        }

        List<AttendanceTimesheet> timesheets = await query
            .Include(timesheet => timesheet.TimesheetStatus)
            .Include(timesheet => timesheet.Contract)
                .ThenInclude(contract => contract.ContractEmployees)
            .Include(timesheet => timesheet.Employee)
                .ThenInclude(employee => employee.EmployeeType)
            .ToListAsync(cancellationToken);

        if (request.GroupBy is GroupByOption.Employee)
        {
            List<EmployeeGroup> employees = timesheets
                .GroupBy(timesheet => timesheet.Employee)
                .Select(grouped =>
                {
                    IEnumerable<TimesheetItem> timesheets = grouped.Select(timesheet =>
                    {
                        ContractEmployee? contractEmployee = timesheet.Contract.ContractEmployees
                            .FirstOrDefault(e => e.EmployeeId == timesheet.EmployeeId);

                        return new TimesheetItem(
                            timesheet.Id,
                            contractEmployee?.Position,
                            contractEmployee?.Workload,
                            timesheet.TimesheetStatusId,
                            timesheet.TimesheetStatus.Name
                        );
                    });

                    bool allApproved = timesheets.All(i => i.Status == "Schválený");

                    return new EmployeeGroup(
                        grouped.Key.Id,
                        allApproved,
                        grouped.Key.PersonalNumber,
                        grouped.Key.FullName,
                        grouped.Key.EmployeeType.Name,
                        timesheets
                    );
                })
                .ToList();

            return TypedResults.Ok(new Response(Employees: employees, Months: []));
        }
        if (request.GroupBy is GroupByOption.Month)
        {
            List<MonthGroup> months = timesheets
                .GroupBy(timesheet => new { timesheet.Year, timesheet.Month })
                .OrderBy(grouped => grouped.Key.Year)
                    .ThenBy(grouped => grouped.Key.Month)
                .Select(monthGroup => new MonthGroup(
                    monthGroup.Key.Year,
                    monthGroup.Key.Month,
                    monthGroup
                        .GroupBy(timesheet => timesheet.Employee)
                        .Select(employeeGroup =>
                        {
                            IEnumerable<TimesheetItem> items = employeeGroup.Select(timesheet =>
                            {
                                ContractEmployee? contractEmployee = timesheet.Contract.ContractEmployees
                                    .FirstOrDefault(e => e.EmployeeId == timesheet.EmployeeId);

                                return new TimesheetItem(
                                    timesheet.Id,
                                    contractEmployee?.Position,
                                    contractEmployee?.Workload,
                                    timesheet.TimesheetStatusId,
                                    timesheet.TimesheetStatus.Name
                                );
                            });

                            bool allApproved = items.All(i => i.Status == "Schválený");

                            return new EmployeeGroup(
                                employeeGroup.Key.Id,
                                allApproved,
                                employeeGroup.Key.PersonalNumber,
                                employeeGroup.Key.FullName,
                                employeeGroup.Key.EmployeeType.Name,
                                items
                            );
                        })
                ))
                .ToList();

            return TypedResults.Ok(new Response(Employees: [], Months: months));
        }

        return TypedResults.Ok(new Response(Employees: [], Months: []));
    }
}
