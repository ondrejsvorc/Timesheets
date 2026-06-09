using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class UpdateTimesheet : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/{id}", Handle)
           .WithSummary("Update Timesheet")
           .WithRequestValidation<Request>();

    public sealed record DayUpdate(
        DateTime Date,
        TimeSpan? ClockIn,
        TimeSpan? ClockOut,
        TimeSpan? BreakStart,
        TimeSpan? BreakEnd,
        decimal? Workload,
        decimal? HoursObligation,
        string? Description,
        IEnumerable<TimeRange>? Schedules
    );

    public sealed record ProjectDayUpdate(DateTime Date, decimal Hours);

    public sealed record ProjectUpdate(
        Guid ContractEmployeeId,
        DateTime? LockedAt,
        Guid? LockedBy,
        IEnumerable<ProjectDayUpdate> Days
    );

    public sealed record Request(IEnumerable<DayUpdate> Days, IEnumerable<ProjectUpdate>? Projects);
    public sealed record Response(Guid Id);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Days).NotEmpty().WithMessage("At least one day must be provided.");
        }
    }

    private static async Task<Results<Ok<Response>, BadRequest<string>, NotFound>> Handle(Guid id, [FromBody] Request request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Data.Models.AttendanceTimesheet? timesheet = await dbContext.AttendanceTimesheets
            .Include(t => t.TimesheetStatus)
            .Include(t => t.Days)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (timesheet is null)
        {
            return TypedResults.NotFound();
        }

        if (timesheet.TimesheetStatus.Name != "Rozpracovaný") // TODO: Use ID instead
        {
            return TypedResults.BadRequest("Timesheet can only be updated when in Draft status.");
        }

        foreach (DayUpdate dayUpdate in request.Days)
        {
            Data.Models.AttendanceDay? day = timesheet.Days.FirstOrDefault(d => d.Date == dayUpdate.Date);
            if (day is null)
            {
                continue;
            }

            day.ClockIn = dayUpdate.ClockIn;
            day.ClockOut = dayUpdate.ClockOut;
            day.BreakStart = dayUpdate.BreakStart;
            day.BreakEnd = dayUpdate.BreakEnd;
            day.Workload = dayUpdate.Workload ?? day.Workload;
            day.HoursObligation = dayUpdate.HoursObligation ?? day.HoursObligation;
            day.Description = dayUpdate.Description;

            if (day.ClockIn.HasValue && day.ClockOut.HasValue)
            {
                decimal workedHours = (decimal)(day.ClockOut.Value - day.ClockIn.Value).TotalHours;
                decimal breakHours = 0m;
                if (day.BreakStart.HasValue && day.BreakEnd.HasValue)
                {
                    breakHours = (decimal)(day.BreakEnd.Value - day.BreakStart.Value).TotalHours;
                }
                day.HoursWithoutBreak = decimal.Round(Math.Max(0, workedHours - breakHours), 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                day.HoursWithoutBreak = 0;
            }

            if (dayUpdate.Schedules is not null)
            {
                day.Schedules = JsonSerializer.Serialize(dayUpdate.Schedules);
            }
        }

        timesheet.UpdatedAt = DateTime.UtcNow;

        if (request.Projects is not null)
        {
            List<Data.Models.ProjectTimesheet> projectTimesheets = await dbContext.ProjectTimesheets
                .Include(pt => pt.Days)
                .Where(pt => pt.EmployeeId == timesheet.EmployeeId && pt.Year == timesheet.Year && pt.Month == timesheet.Month)
                .ToListAsync(cancellationToken);

            foreach (ProjectUpdate projectUpdate in request.Projects)
            {
                Data.Models.ProjectTimesheet? projectTimesheet = projectTimesheets
                    .FirstOrDefault(pt => pt.ContractEmployeeId == projectUpdate.ContractEmployeeId);
                if (projectTimesheet is null)
                {
                    continue;
                }

                projectTimesheet.LockedAt = projectUpdate.LockedAt;
                projectTimesheet.LockedBy = projectUpdate.LockedBy;
                projectTimesheet.UpdatedAt = DateTime.UtcNow;

                foreach (ProjectDayUpdate dayUpdate in projectUpdate.Days)
                {
                    Data.Models.ProjectDay? day = projectTimesheet.Days.FirstOrDefault(d => d.Date == dayUpdate.Date);
                    if (day is null)
                    {
                        continue;
                    }

                    day.Hours = dayUpdate.Hours;
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new Response(timesheet.Id));
    }
}
