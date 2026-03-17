using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Timesheets.Api.Data;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class GetCombinedTimesheet : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/combined", Handle)
           .WithSummary("Get Combined Timesheet");

    public sealed record Request([FromQuery] Guid EmployeeId, [FromQuery] int Year, [FromQuery] int Month);
    public sealed record TimeRange(string Start, string End);
    public sealed record AttendanceItem(string ClockIn, string ClockOut, string BreakStart, string BreakEnd, string Interruptions, decimal NightHours, IEnumerable<TimeRange> Schedules);
    public sealed record CoreDefinition(decimal Workload);
    public sealed record ProjectDefinition(string Id, string RegistrationNumber, string Name, string Position, decimal Workload);
    public sealed record DayItem(string Date, AttendanceItem Attendance, decimal CoreHours, Dictionary<string, decimal> ProjectHours, bool IsHoliday, bool IsWeekend);
    public sealed record Response(int Year, int Month, decimal TotalWorkload, bool HasBaseWorkload, CoreDefinition Core, IEnumerable<ProjectDefinition> Projects, IEnumerable<DayItem> Days);
    private sealed record AttendanceDaySource(DateTime Date, TimeSpan? ClockIn, TimeSpan? ClockOut, TimeSpan? BreakStart, TimeSpan? BreakEnd, decimal Workload, decimal HoursWithoutBreak, bool IsHoliday, string? Description, string Schedules);
    private sealed record ProjectDaySource(DateTime Date, decimal Hours, bool IsHoliday);
    private sealed record ProjectTimesheetSource(Guid ActivityId, Guid ProjectId, string RegistrationNumber, string ProjectName, string Position, decimal Workload, List<ProjectDaySource> Days);

    private static async Task<Results<Ok<Response>, NotFound>> Handle([AsParameters] Request request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var attendanceTimesheet = await dbContext.AttendanceTimesheets
            .AsNoTracking()
            .Where(t => t.EmployeeId == request.EmployeeId && t.Year == request.Year && t.Month == request.Month)
            .Select(t => new
            {
                t.Id,
                Days = t.Days.Select(d => new AttendanceDaySource(d.Date, d.ClockIn, d.ClockOut, d.BreakStart, d.BreakEnd, d.Workload, d.HoursWithoutBreak, d.IsHoliday, d.Description, d.Schedules)).ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (attendanceTimesheet is null)
        {
            return TypedResults.NotFound();
        }

        List<ProjectTimesheetSource> projectTimesheets = await (
            from timesheet in dbContext.ProjectTimesheets.AsNoTracking()
            join contractEmployee in dbContext.ContractEmployees.AsNoTracking() on timesheet.ContractEmployeeId equals contractEmployee.Id
            join contract in dbContext.Contracts.AsNoTracking() on contractEmployee.ContractId equals contract.Id
            join project in dbContext.Projects.AsNoTracking() on contract.ProjectId equals project.Id
            where timesheet.EmployeeId == request.EmployeeId && timesheet.Year == request.Year && timesheet.Month == request.Month
            select new ProjectTimesheetSource(
                contractEmployee.Id,
                project.Id,
                project.RegistrationNumber,
                project.Name,
                contractEmployee.Position,
                timesheet.Workload,
                timesheet.Days.Select(d => new ProjectDaySource(d.Date, d.Hours, d.IsHoliday)).ToList()
            )
        ).ToListAsync(cancellationToken);

        decimal totalProjectWorkload = projectTimesheets.Sum(t => t.Workload);
        decimal? baseWorkload = await GetBaseWorkloadAsync(request.EmployeeId, request.Year, request.Month, dbContext, cancellationToken);
        decimal totalWorkload = baseWorkload ?? 0m;
        decimal coreWorkload = Math.Max(0m, totalWorkload - totalProjectWorkload);
        List<ProjectDefinition> projects = projectTimesheets
            .Select(t => new ProjectDefinition(
                t.ActivityId.ToString(),
                t.RegistrationNumber,
                t.ProjectName,
                t.Position,
                t.Workload
            ))
            .OrderBy(p => p.RegistrationNumber)
            .ThenBy(p => p.Name)
            .ThenBy(p => p.Position)
            .ToList();

        Dictionary<DateOnly, Dictionary<string, decimal>> projectHoursByDate = projectTimesheets
            .SelectMany(timesheet => timesheet.Days.Select(day => new
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
                decimal workedHours = attendanceDay?.HoursWithoutBreak ?? 0m;
                decimal coreHours = Math.Max(0m, workedHours - projectHours.Values.Sum());

                return new DayItem(
                    date.ToString("dd. MM. yyyy"),
                    new AttendanceItem(
                        FormatTime(attendanceDay?.ClockIn),
                        FormatTime(attendanceDay?.ClockOut),
                        FormatTime(attendanceDay?.BreakStart),
                        FormatTime(attendanceDay?.BreakEnd),
                        attendanceDay?.Description ?? string.Empty,
                        0m,
                        ParseSchedules(attendanceDay?.Schedules)
                    ),
                    coreHours,
                    projects.ToDictionary(project => project.Id, project => projectHours.GetValueOrDefault(project.Id)),
                    holidayByDate.GetValueOrDefault(dateOnly),
                    date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
                );
            })
            .ToList();

        return TypedResults.Ok(new Response(request.Year, request.Month, totalWorkload, baseWorkload.HasValue, new CoreDefinition(coreWorkload), projects, days));
    }

    private static async Task<decimal?> GetBaseWorkloadAsync(Guid employeeId, int year, int month, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        DateTime periodStart = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime periodEnd = periodStart.AddMonths(1).AddDays(-1);

        // 1) monthly override (EmployeeWorkload)
        decimal? monthly = await dbContext.EmployeeWorkloads
            .AsNoTracking()
            .Where(w => w.EmployeeId == employeeId && w.Year == year && w.Month == month)
            .Select(w => (decimal?)w.Workload)
            .FirstOrDefaultAsync(cancellationToken);
        if (monthly.HasValue)
        {
            return monthly.Value;
        }

        // 2) core employment (time ranged)
        decimal? workload = await dbContext.CoreEmployments
            .AsNoTracking()
            .Where(e => e.EmployeeId == employeeId)
            .Where(e => e.StartDate <= periodEnd && (e.EndDate == null || e.EndDate >= periodStart))
            .OrderByDescending(e => e.StartDate)
            .Select(e => (decimal?)e.Workload)
            .FirstOrDefaultAsync(cancellationToken);

        return workload;
    }

    private static string FormatTime(TimeSpan? value) => value?.ToString(@"hh\:mm") ?? string.Empty;

    private static IReadOnlyList<TimeRange> ParseSchedules(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<TimeRange>>(value) ?? [];
        }
        catch
        {
            return [];
        }
    }
}