using System.ComponentModel.DataAnnotations.Schema;

namespace Timesheets.Api.Domain.Models;

public sealed class Employee
{
    public Guid Id { get; set; }
    public Guid EmployeeTypeId { get; set; }
    public string PersonalNumber { get; set; } = string.Empty;
    public string? TitleBefore { get; set; }
    public string? TitleAfter { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public bool IsGlobalManager { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public EmployeeType EmployeeType { get; set; } = null!;
    public ICollection<Timesheet> Timesheets { get; set; } = [];
    public ICollection<CoreEmployment> CoreEmployments { get; set; } = [];
    public ICollection<EmployeeWorkload> EmployeeWorkloads { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];

    [NotMapped]
    public string DisplayName
    {
        get
        {
            string before = string.IsNullOrWhiteSpace(TitleBefore) ? string.Empty : TitleBefore.Trim() + " ";
            string after = string.IsNullOrWhiteSpace(TitleAfter) ? string.Empty : " " + TitleAfter.Trim();
            return before + $"{FirstName} {Surname}".Trim() + after;
        }
    }
}
