using CzechHolidays;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Timesheets;

internal static class ProjectTimesheetInitializer
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

    public static async Task EnsureForEmployeeMonthAsync(Guid employeeId, int year, int month, AppDbContext dbContext, ICzechHolidaysFactory holidaysFactory, CancellationToken cancellationToken)
    {
        DateTime periodStart = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime periodEnd = periodStart.AddMonths(1).AddDays(-1);
        List<ContractEmployee> assignments = await dbContext.ContractEmployees
            .AsNoTracking()
            .Include(assignment => assignment.Contract)
            .ThenInclude(contract => contract.Project)
            .Where(assignment => assignment.EmployeeId == employeeId && assignment.StartDate <= periodEnd && (!assignment.EndDate.HasValue || assignment.EndDate >= periodStart))
            .Where(assignment => !assignment.Contract.Project.EndDate.HasValue || assignment.Contract.Project.EndDate >= periodStart)
            .ToListAsync(cancellationToken);

        if (assignments.Count == 0)
        {
            return;
        }

        Guid[] assignmentIds = assignments.Select(assignment => assignment.Id).ToArray();
        HashSet<Guid> existingAssignmentIds = await dbContext.ProjectTimesheets
            .AsNoTracking()
            .Where(timesheet => assignmentIds.Contains(timesheet.ContractEmployeeId) && timesheet.Year == year && timesheet.Month == month)
            .Select(timesheet => timesheet.ContractEmployeeId)
            .ToHashSetAsync(cancellationToken);
        List<ContractEmployee> missingAssignments = assignments.Where(assignment => !existingAssignmentIds.Contains(assignment.Id)).ToList();

        if (missingAssignments.Count == 0)
        {
            return;
        }

        HashSet<DateOnly> holidays = GetHolidays(year, holidaysFactory);
        dbContext.ProjectTimesheets.AddRange(missingAssignments.Select(assignment => Create(assignment, year, month, holidays)));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static async Task<bool> EnsureForAssignmentMonthAsync(ContractEmployee assignment, int year, int month, AppDbContext dbContext, ICzechHolidaysFactory holidaysFactory, CancellationToken cancellationToken)
    {
        if (!IsAssignmentActiveForMonth(assignment, year, month))
        {
            return false;
        }

        bool exists = dbContext.ProjectTimesheets.Local.Any(timesheet => timesheet.ContractEmployeeId == assignment.Id && timesheet.Year == year && timesheet.Month == month)
            || await dbContext.ProjectTimesheets.AnyAsync(timesheet => timesheet.ContractEmployeeId == assignment.Id && timesheet.Year == year && timesheet.Month == month, cancellationToken);

        if (exists)
        {
            return false;
        }

        dbContext.ProjectTimesheets.Add(Create(assignment, year, month, GetHolidays(year, holidaysFactory)));
        return true;
    }

    private static Data.Models.ProjectTimesheet Create(ContractEmployee assignment, int year, int month, HashSet<DateOnly> holidays)
    {
        Data.Models.ProjectTimesheet projectTimesheet = new()
        {
            Id = Guid.CreateVersion7(),
            EmployeeId = assignment.EmployeeId,
            ContractId = assignment.ContractId,
            ContractEmployeeId = assignment.Id,
            TimesheetStatusId = TimesheetWorkflow.DraftStatusId,
            Year = year,
            Month = month,
            Workload = assignment.Workload,
            CreatedAt = DateTime.UtcNow,
        };

        ProjectDateRange range = EffectiveRange(assignment);
        for (int day = 1; day <= DateTime.DaysInMonth(year, month); day++)
        {
            DateTime date = new(year, month, day, 0, 0, 0, DateTimeKind.Utc);
            if (!range.Includes(date))
            {
                continue;
            }

            projectTimesheet.Days.Add(new Data.Models.ProjectDay
            {
                Id = Guid.CreateVersion7(),
                ProjectTimesheetId = projectTimesheet.Id,
                Date = date,
                Hours = 0m,
                IsHoliday = holidays.Contains(DateOnly.FromDateTime(date)),
                Workload = assignment.Workload,
                HoursObligation = TimesheetLogic.CalculateTotalHoursObligation(date, isHoliday: false, workload: assignment.Workload),
            });
        }

        return projectTimesheet;
    }

    private static HashSet<DateOnly> GetHolidays(int year, ICzechHolidaysFactory holidaysFactory) => holidaysFactory.Create(year).Select(holiday => holiday.Date).ToHashSet();
    private static ProjectDateRange EffectiveRange(ContractEmployee assignment) => TimesheetEngine.EffectiveProjectRange(
        assignment.StartDate,
        assignment.EndDate,
        assignment.Contract?.Project?.StartDate ?? assignment.StartDate,
        assignment.Contract?.Project?.EndDate);
    private static DateTime ToUtcDate(DateTime value) => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
