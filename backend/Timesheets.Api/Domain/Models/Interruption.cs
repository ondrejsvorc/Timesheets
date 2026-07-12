namespace Timesheets.Api.Domain.Models;

public sealed class Interruption
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? HoursObligationOverride { get; set; }
}
