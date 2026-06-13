using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Timesheets;

internal static class CombinedTimesheetReviewMapper
{
    public static async Task<TimesheetReview> ReviewAsync(Data.Models.AttendanceTimesheet timesheet, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.Entry(timesheet).Reference(t => t.Employee).LoadAsync(cancellationToken);
        await dbContext.Entry(timesheet).Collection(t => t.Days).LoadAsync(cancellationToken);

        List<Data.Models.ProjectTimesheet> projectTimesheets = await dbContext.ProjectTimesheets
            .Include(pt => pt.Days)
            .Where(pt => pt.EmployeeId == timesheet.EmployeeId && pt.Year == timesheet.Year && pt.Month == timesheet.Month)
            .ToListAsync(cancellationToken);

        decimal totalProjectWorkload = projectTimesheets.Sum(pt => pt.Workload);
        decimal? baseWorkload = await GetBaseWorkloadAsync(timesheet.EmployeeId, timesheet.Year, timesheet.Month, dbContext, cancellationToken);
        decimal coreWorkload = Math.Max(0m, (baseWorkload ?? 0m) - totalProjectWorkload);

        Dictionary<DateOnly, decimal> projectHoursByDate = projectTimesheets
            .SelectMany(pt => pt.Days.Select(day => new { Date = DateOnly.FromDateTime(day.Date), day.Hours }))
            .GroupBy(item => item.Date)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Hours));

        List<CombinedDay> combinedDays = timesheet.Days
            .OrderBy(day => day.Date)
            .Select(day =>
            {
                DateOnly date = DateOnly.FromDateTime(day.Date);
                List<TimeRange> schedules = ParseSchedules(day.Schedules);
                decimal projectHours = projectHoursByDate.GetValueOrDefault(date);

                return new CombinedDay(
                    Date: day.Date,
                    IsHoliday: day.IsHoliday,
                    Workload: day.Workload,
                    CoreWorkload: coreWorkload,
                    WorkedHours: TimesheetLogic.CalculateWorkedHoursFromAttendance(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd),
                    CoreHours: day.CoreHours,
                    ProjectHours: projectHours,
                    StagHours: TimesheetLogic.CalculateStagHours(schedules),
                    HasAttendanceFilled: day.ClockIn is not null || day.ClockOut is not null,
                    SkipAllocationRules: TimesheetInterruptions.SkipAllocationRules(day.Description));
            })
            .ToList();

        CombinedTimesheet combined = new(timesheet.Year, timesheet.Month, coreWorkload, combinedDays);
        AttendanceTimesheet attendance = AttendanceTimesheetReviewMapper.ToReviewInput(timesheet);

        CombinedTimesheetReviewer reviewer = new();
        return reviewer.Review(combined, attendance);
    }

    private static async Task<decimal?> GetBaseWorkloadAsync(Guid employeeId, int year, int month, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        DateTime periodStart = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime periodEnd = periodStart.AddMonths(1).AddDays(-1);

        decimal? monthly = await dbContext.EmployeeWorkloads
            .AsNoTracking()
            .Where(w => w.EmployeeId == employeeId && w.Year == year && w.Month == month)
            .Select(w => (decimal?)w.Workload)
            .FirstOrDefaultAsync(cancellationToken);

        if (monthly.HasValue)
        {
            return monthly.Value;
        }

        return await dbContext.CoreEmployments
            .AsNoTracking()
            .Where(e => e.EmployeeId == employeeId)
            .Where(e => e.StartDate <= periodEnd && (e.EndDate == null || e.EndDate >= periodStart))
            .OrderByDescending(e => e.StartDate)
            .Select(e => (decimal?)e.Workload)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static List<TimeRange> ParseSchedules(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        try
        {
            List<JsonTimeRange>? ranges = JsonSerializer.Deserialize<List<JsonTimeRange>>(value);
            if (ranges is null || ranges.Count == 0)
            {
                return [];
            }

            List<TimeRange> parsed = [];
            foreach (JsonTimeRange range in ranges)
            {
                if (TryParseTime(range.Start, out TimeSpan start) && TryParseTime(range.End, out TimeSpan end))
                {
                    parsed.Add(new TimeRange(start, end));
                }
            }

            return parsed;
        }
        catch
        {
            return [];
        }
    }

    private static bool TryParseTime(string? value, out TimeSpan time)
    {
        time = default;
        return !string.IsNullOrWhiteSpace(value) && TimeSpan.TryParse(value, out time);
    }

    private sealed record JsonTimeRange(string Start, string End);
}
