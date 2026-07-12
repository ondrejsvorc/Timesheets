using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Domain;
using Timesheets.Api.Features.Auth;
using Timesheets.Api.Features.Employees;
using Timesheets.Api.Features.Timesheets.Allocation;

namespace Timesheets.Api.Features.Timesheets.Endpoints;

public sealed class AllocateTimesheet : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/{id}/allocate", Handle)
            .WithSummary("Allocate Timesheet Edit")
            .WithRequestValidation<TimesheetEditRequest>();

    public sealed record ContractPartCell(decimal Hours, bool Locked);
    public sealed record DayResponse(DateTime Date, int?[] Work, int?[] Break, decimal CoreHours, IReadOnlyDictionary<Guid, ContractPartCell> ContractPartCells, bool AttendanceAdjusted);
    public sealed record Response(IReadOnlyList<DayResponse> Days, TimesheetEvaluation Evaluation);

    private static async Task<Results<Ok<Response>, NotFound, ForbidHttpResult>> Handle(Guid id, [FromQuery] int? day, [FromBody] TimesheetEditRequest request, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        LoadedTimesheet? loaded = await TimesheetEngine.LoadAsync(id, dbContext, cancellationToken);
        if (loaded is null)
        {
            return TypedResults.NotFound();
        }
        if ((!user.IsGlobalManagerRole() && user.EmployeeId != loaded.Timesheet.EmployeeId) || loaded.Timesheet.TimesheetStatus.Code != TimesheetStatusCodes.Draft)
        {
            return TypedResults.Forbid();
        }

        EditableTimesheet sheet = TimesheetEngine.BuildEditableTimesheet(loaded, request);

        if (EmployeeTypes.TracksAttendance(loaded.Attendance.EmployeeTypeId))
        {
            NonAcademicTimesheetAllocator allocator = new(loaded, sheet);
            if (day is int dayNumber)
            {
                allocator.AllocateDay(dayNumber);
            }
            else
            {
                allocator.AllocateMonth();
            }
        }
        else
        {
            AcademicTimesheetAllocator allocator = new(loaded, sheet);
            if (day is int dayNumber)
            {
                allocator.AllocateDay(dayNumber);
            }
            else
            {
                allocator.AllocateMonth();
            }
        }

        return TypedResults.Ok(CreateAllocationResponse(loaded, sheet));
    }

    private static Response CreateAllocationResponse(LoadedTimesheet loaded, EditableTimesheet sheet)
    {
        List<DayResponse> allocation = sheet.Days
            .Select(day => new DayResponse(
                Date: day.Date,
                Work: [ConvertToMinutes(day.ClockIn), ConvertToMinutes(day.ClockOut)],
                Break: [ConvertToMinutes(day.BreakStart), ConvertToMinutes(day.BreakEnd)],
                CoreHours: day.CoreHours,
                ContractPartCells: sheet.ContractParts.ToDictionary(
                    project => project.Id,
                    project => new ContractPartCell(
                        day.ContractPartHours.GetValueOrDefault(project.Id),
                        day.ContractPartHoursFixed.GetValueOrDefault(project.Id))),
                AttendanceAdjusted: day.AttendanceAdjusted))
            .ToList();
        return new Response(Days: allocation, Evaluation: TimesheetEngine.Evaluate(loaded, sheet));
    }

    private static int? ConvertToMinutes(TimeSpan? value) => value.HasValue ? (int)Math.Round(value.Value.TotalMinutes) : null;
}
