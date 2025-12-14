using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class CreateTimesheet : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/", Handle)
           .WithSummary("Create Timesheet")
           .WithRequestValidation<Request>();

    public sealed record Request(Guid EmployeeId, Guid ContractId, int Year, int Month);
    public sealed record Response(Guid Id);
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Month).InclusiveBetween(1, 12);
        }
    }

    private static async Task<Results<Created<Response>, BadRequest<string>, NotFound>> Handle([FromBody] Request request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        bool employeeExists = await dbContext.Employees
            .AsNoTracking()
            .AnyAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (!employeeExists)
        {
            return TypedResults.BadRequest("Employee not found.");
        }

        bool contractExists = await dbContext.Contracts
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.ContractId, cancellationToken);

        if (!contractExists)
        {
            return TypedResults.BadRequest("Contract not found.");
        }

        bool timesheetExists = await dbContext.AttendanceTimesheets
            .AsNoTracking()
            .AnyAsync(t => t.EmployeeId == request.EmployeeId
                && t.ContractId == request.ContractId
                && t.Year == request.Year
                && t.Month == request.Month, cancellationToken);

        if (timesheetExists)
        {
            return TypedResults.BadRequest("Timesheet for this employee, contract, and period already exists.");
        }

        TimesheetStatus? draftStatus = await dbContext.TimesheetStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Name == "Rozpracovaný", cancellationToken);

        if (draftStatus is null)
        {
            return TypedResults.BadRequest("Draft status not found in database.");
        }

        Data.Models.AttendanceTimesheet timesheet = new()
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            ContractId = request.ContractId,
            TimesheetStatusId = draftStatus.Id,
            Year = request.Year,
            Month = request.Month
        };

        for (int day = 1; day <= DateTime.DaysInMonth(request.Year, request.Month); day++)
        {
            DateTime date = new(request.Year, request.Month, day);
            timesheet.Days.Add(new Data.Models.AttendanceDay()
            {
                Id = Guid.NewGuid(),
                AttendanceTimesheetId = timesheet.Id,
                Date = date,
                ClockIn = null,
                ClockOut = null,
                BreakStart = null,
                BreakEnd = null,
                Workload = null,
                HoursWithoutBreak = 0, // TODO
                HoursObligation = 0, // TODO
                IsHoliday = false, // TODO
                Description = null, // TODO
                Schedules = "[]"
            });
        }

        dbContext.AttendanceTimesheets.Add(timesheet);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/timesheets/{timesheet.Id}", new Response(timesheet.Id));
    }
}

