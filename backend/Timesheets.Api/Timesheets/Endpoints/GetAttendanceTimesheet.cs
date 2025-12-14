using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Timesheets.Endpoints;

// TODO
public sealed class GetAttendanceTimesheet : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}", Handle)
           .WithSummary("Get Attendance Timesheet");

    public sealed record DayItem(
        Guid Id,
        DateTime Date,
        TimeSpan? ClockIn,
        TimeSpan? ClockOut,
        TimeSpan? BreakStart,
        TimeSpan? BreakEnd,
        decimal? Workload,
        decimal HoursWithoutBreak,
        decimal HoursObligation,
        bool IsHoliday,
        string? Description,
        string Schedules,
        IEnumerable<InterruptionItem> Interruptions
    );

    public sealed record InterruptionItem(
        Guid Id,
        string Name,
        string? Description
    );

    public sealed record Response(
        Guid Id,
        Guid EmployeeId,
        string EmployeeName,
        Guid ContractId,
        string ContractName,
        Guid TimesheetStatusId,
        string TimesheetStatus,
        Guid? ApprovedBy,
        int Year,
        int Month,
        DateTime? SubmittedAt,
        DateTime? ApprovedAt,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        IEnumerable<DayItem> Days
    );

    private static async Task<Results<Ok<Response>, NotFound>> Handle(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var timesheet = await dbContext.AttendanceTimesheets
            .AsNoTracking()
            .Include(t => t.Employee)
            .Include(t => t.Contract)
            .Include(t => t.TimesheetStatus)
            .Include(t => t.Days)
                .ThenInclude(d => d.DayInterruptions)
                    .ThenInclude(di => di.Interruption)
            .Where(t => t.Id == id)
            .Select(t => new Response(
                t.Id,
                t.EmployeeId,
                t.Employee.FullName,
                t.ContractId,
                t.Contract.Name,
                t.TimesheetStatusId,
                t.TimesheetStatus.Name,
                t.ApprovedBy,
                t.Year,
                t.Month,
                t.SubmittedAt,
                t.ApprovedAt,
                t.CreatedAt,
                t.UpdatedAt,
                t.Days.Select(d => new DayItem(
                    d.Id,
                    d.Date,
                    d.ClockIn,
                    d.ClockOut,
                    d.BreakStart,
                    d.BreakEnd,
                    d.Workload,
                    d.HoursWithoutBreak,
                    d.HoursObligation,
                    d.IsHoliday,
                    d.Description,
                    d.Schedules,
                    d.DayInterruptions.Select(di => new InterruptionItem(
                        di.Interruption.Id,
                        di.Interruption.Name,
                        di.Interruption.Description
                    ))
                ))
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (timesheet is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(timesheet);
    }
}

