using CzechHolidays;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Features.Timesheets;

internal static class ContractPartInitializer
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

        Guid timesheetId = await TimesheetBootstrap.EnsureMonthTimesheetIdAsync(dbContext, employeeId, year, month, cancellationToken);
        Guid[] assignmentIds = assignments.Select(assignment => assignment.Id).ToArray();
        HashSet<Guid> existingAssignmentIds = await dbContext.ContractParts
            .AsNoTracking()
            .Where(part => part.TimesheetId == timesheetId && assignmentIds.Contains(part.ContractEmployeeId))
            .Select(part => part.ContractEmployeeId)
            .ToHashSetAsync(cancellationToken);
        List<ContractEmployee> missingAssignments = assignments.Where(assignment => !existingAssignmentIds.Contains(assignment.Id)).ToList();

        if (missingAssignments.Count == 0)
        {
            return;
        }

        HashSet<DateOnly> holidays = GetHolidays(year, holidaysFactory);
        dbContext.ContractParts.AddRange(missingAssignments.Select(assignment => Create(assignment, year, month, holidays, timesheetId)));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static async Task<bool> EnsureForAssignmentMonthAsync(ContractEmployee assignment, int year, int month, AppDbContext dbContext, ICzechHolidaysFactory holidaysFactory, CancellationToken cancellationToken)
    {
        if (!IsAssignmentActiveForMonth(assignment, year, month))
        {
            return false;
        }

        Guid timesheetId = await TimesheetBootstrap.EnsureMonthTimesheetIdAsync(dbContext, assignment.EmployeeId, year, month, cancellationToken);
        bool exists = dbContext.ContractParts.Local.Any(part => part.TimesheetId == timesheetId && part.ContractEmployeeId == assignment.Id)
            || await dbContext.ContractParts.AnyAsync(part => part.TimesheetId == timesheetId && part.ContractEmployeeId == assignment.Id, cancellationToken);

        if (exists)
        {
            return false;
        }

        dbContext.ContractParts.Add(Create(assignment, year, month, GetHolidays(year, holidaysFactory), timesheetId));
        return true;
    }

    private static ContractPart Create(ContractEmployee assignment, int year, int month, HashSet<DateOnly> holidays, Guid timesheetId)
    {
        ContractPart contractPart = new()
        {
            Id = Guid.CreateVersion7(),
            TimesheetId = timesheetId,
            ContractEmployeeId = assignment.Id,
            TimesheetStatusId = TimesheetWorkflow.DraftStatusId,
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

            bool isHoliday = holidays.Contains(DateOnly.FromDateTime(date));
            contractPart.Days.Add(new ContractPartDay
            {
                Id = Guid.CreateVersion7(),
                ContractPartId = contractPart.Id,
                Date = date,
                Hours = 0m,
                IsHoliday = isHoliday,
                HoursObligation = TimesheetLogic.CalculateTotalHoursObligation(date, isHoliday, assignment.Workload),
            });
        }

        return contractPart;
    }

    private static HashSet<DateOnly> GetHolidays(int year, ICzechHolidaysFactory holidaysFactory) => holidaysFactory.Create(year).Select(holiday => holiday.Date).ToHashSet();
    private static ProjectDateRange EffectiveRange(ContractEmployee assignment) => TimesheetEngine.EffectiveProjectRange(
        assignment.StartDate,
        assignment.EndDate,
        assignment.Contract?.Project?.StartDate ?? assignment.StartDate,
        assignment.Contract?.Project?.EndDate);
    private static DateTime ToUtcDate(DateTime value) => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
