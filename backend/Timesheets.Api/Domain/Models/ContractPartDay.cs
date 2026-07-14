namespace Timesheets.Api.Domain.Models;

public sealed class ContractPartDay
{
    public Guid Id { get; set; }
    public Guid ContractPartId { get; set; }
    public DateTime Date { get; set; }
    public decimal Hours { get; set; }
    public bool HoursLocked { get; set; }
    public bool IsHoliday { get; set; }
    public decimal HoursObligation { get; set; }

    public ContractPart ContractPart { get; set; } = null!;
}
