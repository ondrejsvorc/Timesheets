namespace Timesheets.Api.Domain.Models;

public sealed class DayInterruption
{
    public Guid Id { get; set; }
    public Guid AttendanceDayId { get; set; }
    public Guid InterruptionId { get; set; }

    public AttendanceDay AttendanceDay { get; set; } = null!;
    public Interruption Interruption { get; set; } = null!;
}
