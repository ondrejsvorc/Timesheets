using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class GetCombinedTimesheet : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/combined", Handle)
           .WithSummary("Get Combined Timesheet");

    public sealed record Request([FromQuery] Guid EmployeeId, [FromQuery] int Year, [FromQuery] int Month);
    public sealed record ProjectDefinition(string Id, string Name, decimal Workload, DateTime? LockedAt, Guid? LockedBy);
    public sealed record DayItem(int Day, int?[] Work, int?[] Break, decimal[] ProjectHours, bool IsHoliday, bool IsWeekend, string? Note, IReadOnlyList<int[]>? Schedules);
    public sealed record Response(Guid Id, int Year, int Month, decimal TotalWorkload, decimal CoreWorkload, IEnumerable<ProjectDefinition> Projects, IEnumerable<DayItem> Days);
    private sealed record AttendanceDaySource(DateTime Date, TimeSpan? ClockIn, TimeSpan? ClockOut, TimeSpan? BreakStart, TimeSpan? BreakEnd, decimal Workload, decimal HoursWithoutBreak, bool IsHoliday, string? Description, string Schedules);
    private sealed record ProjectDaySource(DateTime Date, decimal Hours, bool IsHoliday);
    private sealed record ProjectTimesheetSource(Guid ActivityId, Guid ProjectId, string RegistrationNumber, string ProjectName, string Position, decimal Workload, DateTime? LockedAt, Guid? LockedBy, List<ProjectDaySource> Days);

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
                timesheet.LockedAt,
                timesheet.LockedBy,
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
                t.ProjectName,
                t.Workload,
                t.LockedAt,
                t.LockedBy
            ))
            .OrderBy(p => p.Name)
            .ToList();

        Dictionary<string, int> projectIndexById = projects
            .Select((p, index) => new { p.Id, Index = index })
            .ToDictionary(x => x.Id, x => x.Index);

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
                decimal[] projectHoursArray = new decimal[projects.Count];
                foreach ((string projectId, decimal hours) in projectHours)
                {
                    if (projectIndexById.TryGetValue(projectId, out int index))
                    {
                        projectHoursArray[index] = hours;
                    }
                }

                return new DayItem(
                    dayNumber,
                    [ToMinutes(attendanceDay?.ClockIn), ToMinutes(attendanceDay?.ClockOut)],
                    [ToMinutes(attendanceDay?.BreakStart), ToMinutes(attendanceDay?.BreakEnd)],
                    projectHoursArray,
                    holidayByDate.GetValueOrDefault(dateOnly),
                    date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
                    string.IsNullOrWhiteSpace(attendanceDay?.Description) ? null : attendanceDay.Description,
                    ParseSchedules(attendanceDay?.Schedules)
                );
            })
            .ToList();

        return TypedResults.Ok(new Response(attendanceTimesheet.Id, request.Year, request.Month, totalWorkload, coreWorkload, projects, days));
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

    private static int? ToMinutes(TimeSpan? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return (int)Math.Round(value.Value.TotalMinutes);
    }

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
