using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain.Models;

namespace Timesheets.Api.Domain;

public static class TimesheetBootstrap
{
    public static async Task<Guid> EnsureMonthTimesheetIdAsync(AppDbContext db, Guid employeeId, int year, int month, CancellationToken cancellationToken)
    {
        Guid? localTimesheetId = db.Timesheets.Local
            .Where(timesheet => timesheet.EmployeeId == employeeId && timesheet.Year == year && timesheet.Month == month)
            .Select(timesheet => (Guid?)timesheet.Id)
            .FirstOrDefault();

        if (localTimesheetId.HasValue)
        {
            return localTimesheetId.Value;
        }

        Guid? timesheetId = await db.Timesheets
            .AsNoTracking()
            .Where(timesheet => timesheet.EmployeeId == employeeId && timesheet.Year == year && timesheet.Month == month)
            .Select(timesheet => (Guid?)timesheet.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (timesheetId.HasValue)
        {
            return timesheetId.Value;
        }

        Guid employeeTypeId = await db.Employees
            .Where(employee => employee.Id == employeeId)
            .Select(employee => employee.EmployeeTypeId)
            .SingleAsync(cancellationToken);

        Timesheet timesheet = new()
        {
            Id = Guid.CreateVersion7(),
            EmployeeId = employeeId,
            TimesheetStatusId = TimesheetStatus.DraftId,
            Year = year,
            Month = month,
            CreatedAt = DateTime.UtcNow,
        };
        AddMonth(db, timesheet, employeeTypeId);
        return timesheet.Id;
    }

    public static void AddMonth(AppDbContext db, Timesheet timesheet, Guid employeeTypeId)
    {
        db.Timesheets.Add(timesheet);
        db.Attendances.Add(new Attendance
        {
            Id = timesheet.Id,
            TimesheetId = timesheet.Id,
            EmployeeTypeId = employeeTypeId,
        });
    }

    public static void AddMonthWithDays(AppDbContext db, Timesheet timesheet, Guid employeeTypeId, IEnumerable<Models.AttendanceDay> days)
    {
        AddMonth(db, timesheet, employeeTypeId);
        foreach (Models.AttendanceDay day in days)
        {
            day.AttendanceId = timesheet.Id;
            db.AttendanceDays.Add(day);
        }
    }
}
