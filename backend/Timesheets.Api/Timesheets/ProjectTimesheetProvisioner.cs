using CzechHolidays;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Timesheets;

internal static class ProjectTimesheetProvisioner
{
    public static bool IsAssignmentActiveForMonth(ContractEmployee assignment, int year, int month)
    {
        DateTime periodStart = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime periodEnd = periodStart.AddMonths(1).AddDays(-1);
        DateTime start = ToUtcDate(assignment.StartDate);

        if (start > periodEnd)
        {
            return false;
        }

        if (!assignment.EndDate.HasValue)
        {
            return true;
        }

        DateTime end = ToUtcDate(assignment.EndDate.Value);
        return end >= periodStart;
    }

    public static async Task EnsureForEmployeeMonthAsync(
        Guid employeeId,
        int year,
        int month,
        AppDbContext dbContext,
        ICzechHolidaysFactory holidaysFactory,
        CancellationToken cancellationToken)
    {
        List<ContractEmployee> assignments = await dbContext.ContractEmployees
            .AsNoTracking()
            .Where(ce => ce.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);

        bool anyCreated = false;
        foreach (ContractEmployee assignment in assignments.Where(a => IsAssignmentActiveForMonth(a, year, month)))
        {
            anyCreated |= await EnsureForAssignmentMonthAsync(assignment, year, month, dbContext, holidaysFactory, cancellationToken);
        }

        if (anyCreated)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public static async Task<bool> EnsureForAssignmentMonthAsync(
        ContractEmployee assignment,
        int year,
        int month,
        AppDbContext dbContext,
        ICzechHolidaysFactory holidaysFactory,
        CancellationToken cancellationToken)
    {
        if (!IsAssignmentActiveForMonth(assignment, year, month))
        {
            return false;
        }

        bool exists = await dbContext.ProjectTimesheets
            .AnyAsync(
                t => t.ContractEmployeeId == assignment.Id && t.Year == year && t.Month == month,
                cancellationToken);

        if (exists)
        {
            return false;
        }

        HashSet<DateOnly> holidays = holidaysFactory.Create(year).Select(h => h.Date).ToHashSet();
        Data.Models.ProjectTimesheet projectTimesheet = new()
        {
            Id = Guid.NewGuid(),
            EmployeeId = assignment.EmployeeId,
            ContractId = assignment.ContractId,
            ContractEmployeeId = assignment.Id,
            TimesheetStatusId = TimesheetWorkflowConstants.DraftStatusId,
            Year = year,
            Month = month,
            Workload = assignment.Workload,
            CreatedAt = DateTime.UtcNow,
        };

        for (int day = 1; day <= DateTime.DaysInMonth(year, month); day++)
        {
            DateTime date = new(year, month, day, 0, 0, 0, DateTimeKind.Utc);
            projectTimesheet.Days.Add(new Data.Models.ProjectDay
            {
                Id = Guid.NewGuid(),
                ProjectTimesheetId = projectTimesheet.Id,
                Date = date,
                Hours = 0m,
                IsHoliday = holidays.Contains(DateOnly.FromDateTime(date)),
                Workload = assignment.Workload,
                HoursObligation = 0m,
            });
        }

        dbContext.ProjectTimesheets.Add(projectTimesheet);
        return true;
    }

    private static DateTime ToUtcDate(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
