namespace Timesheets.Api.Domain.Models;

public sealed class AttendanceDay
{
    public Guid Id { get; set; }
    public Guid AttendanceId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan? ClockIn { get; set; }
    public TimeSpan? ClockOut { get; set; }
    public TimeSpan? BreakStart { get; set; }
    public TimeSpan? BreakEnd { get; set; }
    public decimal Workload { get; set; }
    public decimal HoursWithoutBreak { get; set; }
    public decimal HoursObligation { get; set; }
    public decimal CoreHours { get; set; }
    public bool IsHoliday { get; set; }
    public string? Description { get; set; }
    public string Schedules { get; set; } = "[]";

    public Attendance Attendance { get; set; } = null!;
    public ICollection<DayInterruption> DayInterruptions { get; set; } = [];
}
