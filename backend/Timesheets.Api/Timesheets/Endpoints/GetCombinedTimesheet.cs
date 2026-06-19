using System.Text.Json;
using CzechHolidays;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Auth;
using Timesheets.Api.Data;
using Timesheets.Api.Employees;
using Timesheets.Api.Timesheets;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class GetCombinedTimesheet : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/combined", Handle)
           .WithSummary("Get Combined Timesheet");

    public sealed record Request([FromQuery] Guid EmployeeId, [FromQuery] int Year, [FromQuery] int Month);
    public sealed record ProjectDefinition(string Id, string RegistrationNumber, string Name, string Position, decimal Workload, bool Locked, bool[] ActiveDays);
    public sealed record DayItem(int Day, int?[] Work, int?[] Break, decimal CoreHours, decimal[] ProjectHours, bool IsHoliday, bool IsWeekend, string? Note, IReadOnlyList<int[]>? Schedules);
    public sealed record Response(Guid Id, int Year, int Month, decimal TotalWorkload, decimal CoreWorkload, bool TracksAttendance, IEnumerable<ProjectDefinition> Projects, IEnumerable<DayItem> Days);
    private sealed record AttendanceDaySource(DateTime Date, TimeSpan? ClockIn, TimeSpan? ClockOut, TimeSpan? BreakStart, TimeSpan? BreakEnd, decimal Workload, decimal HoursWithoutBreak, decimal CoreHours, bool IsHoliday, string? Description, string Schedules);
    private sealed record ProjectDaySource(DateTime Date, decimal Hours, bool IsHoliday);
    private sealed record ProjectTimesheetSource(Guid ActivityId, Guid ProjectId, string RegistrationNumber, string ProjectName, string Position, decimal Workload, DateTime? LockedAt, ProjectDateRange Range, List<ProjectDaySource> Days);
    private sealed record ProjectTimesheetRow(Guid ActivityId, Guid ProjectId, string RegistrationNumber, string ProjectName, string Position, decimal Workload, DateTime? LockedAt, DateTime AssignmentStartDate, DateTime? AssignmentEndDate, DateTime ProjectStartDate, DateTime? ProjectEndDate, List<ProjectDaySource> Days);

    private static async Task<Results<Ok<Response>, NotFound, ForbidHttpResult>> Handle([AsParameters] Request request, AppDbContext dbContext, ICzechHolidaysFactory holidaysFactory, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!await user.CanAccessEmployeeAsync(request.EmployeeId, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var attendanceTimesheet = await dbContext.AttendanceTimesheets
            .AsNoTracking()
            .Where(t => t.EmployeeId == request.EmployeeId && t.Year == request.Year && t.Month == request.Month)
            .Select(t => new
            {
                t.Id,
                EmployeeTypeId = t.EmployeeTypeId ?? t.Employee.EmployeeTypeId,
                Days = t.Days.Select(d => new AttendanceDaySource(d.Date, d.ClockIn, d.ClockOut, d.BreakStart, d.BreakEnd, d.Workload, d.HoursWithoutBreak, d.CoreHours, d.IsHoliday, d.Description, d.Schedules)).ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (attendanceTimesheet is null)
        {
            return TypedResults.NotFound();
        }

        await ProjectTimesheetInitializer.EnsureForEmployeeMonthAsync(request.EmployeeId, request.Year, request.Month, dbContext, holidaysFactory, cancellationToken);

        List<ProjectTimesheetRow> projectTimesheetRows = await (
            from timesheet in dbContext.ProjectTimesheets.AsNoTracking()
            join contractEmployee in dbContext.ContractEmployees.AsNoTracking() on timesheet.ContractEmployeeId equals contractEmployee.Id
            join contract in dbContext.Contracts.AsNoTracking() on contractEmployee.ContractId equals contract.Id
            join project in dbContext.Projects.AsNoTracking() on contract.ProjectId equals project.Id
            where timesheet.EmployeeId == request.EmployeeId && timesheet.Year == request.Year && timesheet.Month == request.Month
            select new ProjectTimesheetRow(
                contractEmployee.Id,
                project.Id,
                contract.RegistrationNumber,
                project.Name,
                contractEmployee.Position,
                timesheet.Workload,
                timesheet.LockedAt,
                contractEmployee.StartDate,
                contractEmployee.EndDate,
                project.StartDate,
                project.EndDate,
                timesheet.Days.Select(d => new ProjectDaySource(d.Date, d.Hours, d.IsHoliday)).ToList()
            )
        ).ToListAsync(cancellationToken);
        List<ProjectTimesheetSource> projectTimesheets = projectTimesheetRows
            .Select(row => new ProjectTimesheetSource(
                row.ActivityId,
                row.ProjectId,
                row.RegistrationNumber,
                row.ProjectName,
                row.Position,
                row.Workload,
                row.LockedAt,
                TimesheetEngine.EffectiveProjectRange(row.AssignmentStartDate, row.AssignmentEndDate, row.ProjectStartDate, row.ProjectEndDate),
                row.Days))
            .ToList();

        decimal totalProjectWorkload = projectTimesheets.Sum(t => t.Workload);
        decimal totalWorkload = await TimesheetWorkloads.GetAsync(request.EmployeeId, request.Year, request.Month, dbContext, cancellationToken);
        decimal coreWorkload = Math.Max(0m, totalWorkload - totalProjectWorkload);
        bool tracksAttendance = EmployeeTypes.TracksAttendance(attendanceTimesheet.EmployeeTypeId);
        List<ProjectColumn> projectStates = projectTimesheets
            .Select(t => new ProjectColumn(t.ActivityId, t.Workload, t.LockedAt is not null, t.Range))
            .ToList();
        List<ProjectDefinition> projects = projectTimesheets
            .Select(t => new ProjectDefinition(
                t.ActivityId.ToString(),
                t.RegistrationNumber,
                t.ProjectName,
                t.Position,
                t.Workload,
                t.LockedAt is not null,
                BuildActiveDays(request.Year, request.Month, t.Range)
            ))
            .OrderBy(p => p.Name)
            .ToList();

        Dictionary<string, int> projectIndexById = projects
            .Select((p, index) => new { p.Id, Index = index })
            .ToDictionary(x => x.Id, x => x.Index);

        Dictionary<DateOnly, Dictionary<string, decimal>> projectHoursByDate = projectTimesheets
            .SelectMany(timesheet => timesheet.Days.Where(day => timesheet.Range.Includes(day.Date)).Select(day => new
            {
                Date = DateOnly.FromDateTime(day.Date),
                timesheet.ActivityId,
                day.Hours
            }))
            .GroupBy(item => item.Date)
            .ToDictionary(
                group => group.Key,
                group => group
                    .GroupBy(item => item.ActivityId.ToString())
                    .ToDictionary(projectGroup => projectGroup.Key, projectGroup => projectGroup.Sum(item => item.Hours))
            );

        Dictionary<DateOnly, bool> holidayByDate = attendanceTimesheet.Days
            .ToDictionary(day => DateOnly.FromDateTime(day.Date), day => day.IsHoliday);

        foreach (var projectDay in projectTimesheets.SelectMany(timesheet => timesheet.Days))
        {
            DateOnly date = DateOnly.FromDateTime(projectDay.Date);
            if (!holidayByDate.ContainsKey(date))
            {
                holidayByDate[date] = projectDay.IsHoliday;
            }
        }

        Dictionary<DateOnly, AttendanceDaySource> attendanceDaysByDate = attendanceTimesheet.Days
            .ToDictionary(day => DateOnly.FromDateTime(day.Date), day => day);

        List<DayItem> days = Enumerable.Range(1, DateTime.DaysInMonth(request.Year, request.Month))
            .Select(dayNumber =>
            {
                DateTime date = new(request.Year, request.Month, dayNumber, 0, 0, 0, DateTimeKind.Utc);
                DateOnly dateOnly = DateOnly.FromDateTime(date);
                AttendanceDaySource? attendanceDay = attendanceDaysByDate.GetValueOrDefault(dateOnly);
                Dictionary<string, decimal> projectHours = projectHoursByDate.GetValueOrDefault(dateOnly) ?? [];
                decimal[] projectHoursArray = new decimal[projects.Count];
                foreach ((string projectId, decimal hours) in projectHours)
                {
                    if (projectIndexById.TryGetValue(projectId, out int index))
                    {
                        projectHoursArray[index] = hours;
                    }
                }

                decimal coreHours = attendanceDay?.CoreHours ?? 0m;
                if (attendanceDay is not null && !string.IsNullOrWhiteSpace(attendanceDay.Description))
                {
                    EditableTimesheetDay dayState = new()
                    {
                        Date = date,
                        ClockIn = attendanceDay.ClockIn,
                        ClockOut = attendanceDay.ClockOut,
                        BreakStart = attendanceDay.BreakStart,
                        BreakEnd = attendanceDay.BreakEnd,
                        Description = attendanceDay.Description,
                        Schedules = [],
                        IsHoliday = holidayByDate.GetValueOrDefault(dateOnly),
                        CoreHours = coreHours,
                        CoreHoursFixed = false,
                        ProjectHours = projects.ToDictionary(project => Guid.Parse(project.Id), project => projectHoursArray[projectIndexById[project.Id]]),
                        ProjectHoursFixed = projects.ToDictionary(project => Guid.Parse(project.Id), _ => false)
                    };
                    TimesheetInterruptionHours.ApplyToDayState(dayState, projectStates, totalWorkload, tracksAttendance);
                    coreHours = dayState.CoreHours;
                    foreach (ProjectDefinition project in projects)
                    {
                        projectHoursArray[projectIndexById[project.Id]] = dayState.ProjectHours[Guid.Parse(project.Id)];
                    }
                }

                return new DayItem(
                    dayNumber,
                    [ToMinutes(attendanceDay?.ClockIn), ToMinutes(attendanceDay?.ClockOut)],
                    [ToMinutes(attendanceDay?.BreakStart), ToMinutes(attendanceDay?.BreakEnd)],
                    coreHours,
                    projectHoursArray,
                    holidayByDate.GetValueOrDefault(dateOnly),
                    date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
                    string.IsNullOrWhiteSpace(attendanceDay?.Description) ? null : attendanceDay.Description,
                    ParseSchedules(attendanceDay?.Schedules)
                );
            })
            .ToList();

        return TypedResults.Ok(new Response(attendanceTimesheet.Id, request.Year, request.Month, totalWorkload, coreWorkload, tracksAttendance, projects, days));
    }

    private static int? ToMinutes(TimeSpan? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return (int)Math.Round(value.Value.TotalMinutes);
    }

    private static bool[] BuildActiveDays(int year, int month, ProjectDateRange range) => Enumerable.Range(1, DateTime.DaysInMonth(year, month))
        .Select(day => range.Includes(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc)))
        .ToArray();

    private static IReadOnlyList<int[]>? ParseSchedules(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            List<JsonTimeRange>? ranges = JsonSerializer.Deserialize<List<JsonTimeRange>>(value);
            if (ranges is null || ranges.Count == 0)
            {
                return null;
            }

            List<int[]> parsed = [];
            foreach (JsonTimeRange range in ranges)
            {
                if (!TryParseMinutes(range.Start, out int start) || !TryParseMinutes(range.End, out int end))
                {
                    continue;
                }

                parsed.Add([start, end]);
            }

            return parsed.Count > 0 ? parsed : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool TryParseMinutes(string value, out int minutes)
    {
        minutes = 0;
        if (!TimeSpan.TryParse(value, out TimeSpan parsed))
        {
            return false;
        }

        minutes = (int)Math.Round(parsed.TotalMinutes);
        return true;
    }

    private sealed record JsonTimeRange(string Start, string End);
}
