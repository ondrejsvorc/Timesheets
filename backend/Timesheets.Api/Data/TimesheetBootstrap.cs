using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Data;

// ponytail: dual-write shadow Timesheet + Attendance until AttendanceTimesheet is removed (step 4g).
public static class TimesheetBootstrap
{
    public static void AddLegacyMonth(AppDbContext db, AttendanceTimesheet legacy)
    {
        Guid employeeTypeId = legacy.EmployeeTypeId
            ?? throw new InvalidOperationException("EmployeeTypeId is required when creating Attendance.");

        db.Timesheets.Add(new Timesheet
        {
            Id = legacy.Id,
            EmployeeId = legacy.EmployeeId,
            TimesheetStatusId = legacy.TimesheetStatusId,
            ApprovedBy = legacy.ApprovedBy,
            Year = legacy.Year,
            Month = legacy.Month,
            SubmittedAt = legacy.SubmittedAt,
            ApprovedAt = legacy.ApprovedAt,
            CreatedAt = legacy.CreatedAt,
            UpdatedAt = legacy.UpdatedAt,
        });

        db.Attendances.Add(new Attendance
        {
            Id = legacy.Id,
            TimesheetId = legacy.Id,
            EmployeeTypeId = employeeTypeId,
        });

        db.AttendanceTimesheets.Add(legacy);
    }

    public static void AddLegacyMonthWithDays(AppDbContext db, AttendanceTimesheet legacy, IEnumerable<AttendanceDay> days)
    {
        AddLegacyMonth(db, legacy);
        foreach (AttendanceDay day in days)
        {
            day.AttendanceId = legacy.Id;
            db.AttendanceDays.Add(day);
        }
    }
}
