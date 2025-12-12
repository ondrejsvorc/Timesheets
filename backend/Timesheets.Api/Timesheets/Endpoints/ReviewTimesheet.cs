using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using System.Text.Json;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class ReviewTimesheet : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/{id}/review", Handle)
           .WithSummary("Review Timesheet");

    public sealed record Response(TimesheetReview review);

    private static async Task<Results<Ok<Response>, NotFound>> Handle(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Data.Models.AttendanceTimesheet? timesheet = await dbContext.AttendanceTimesheets
            .AsNoTracking()
            .Include(t => t.Days)
                .ThenInclude(d => d.DayInterruptions)
            .Include(t => t.Employee)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (timesheet is null)
        {
            return TypedResults.NotFound();
        }

        List<AttendanceDay> attendanceDays = timesheet.Days.Select(d =>
        {
            List<TimeRange> schedules = string.IsNullOrWhiteSpace(d.Schedules) ? [] : JsonSerializer.Deserialize<List<TimeRange>>(d.Schedules) ?? [];

            return new AttendanceDay(
                Date: d.Date,
                ClockIn: d.ClockIn,
                ClockOut: d.ClockOut,
                BreakStart: d.BreakStart,
                BreakEnd: d.BreakEnd,
                OtherInterruption: null, // TODO
                Schedules: schedules,
                IsHoliday: d.IsHoliday,
                Workload: d.Workload ?? 0
            );
        }).ToList();

        AttendanceTimesheet attendanceTimesheet = new(
            EmployeePersonalNumber: timesheet.Employee.PersonalNumber ?? 0, // TODO
            EmployeeName: timesheet.Employee.FullName,
            Workload: attendanceDays.FirstOrDefault()?.Workload ?? 0,
            Year: timesheet.Year,
            Month: timesheet.Month,
            Days: attendanceDays
        );

        AttendanceTimesheetReviewer reviewer = new();
        TimesheetReview review = reviewer.Review(attendanceTimesheet);

        return TypedResults.Ok(new Response(review));
    }
}

