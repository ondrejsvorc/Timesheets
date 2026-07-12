using System.ComponentModel.DataAnnotations.Schema;
using Timesheets.Api.Common;

namespace Timesheets.Api.Domain.Models;

public sealed class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string NormalizedRegistrationNumber { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ProjectManager> ProjectManagers { get; set; } = [];
    public ICollection<Contract> Contracts { get; set; } = [];

    public bool IsArchived() => ArchivedAt.HasValue;

    public ProjectStatus GetStatus(DateOnly date)
    {
        if (IsArchived())
        {
            return "archived";
        }

        DateOnly startDate = DateOnly.FromDateTime(StartDate);

        if (date < startDate)
        {
            return "inactive";
        }

        if (EndDate is not null && date > DateOnly.FromDateTime(EndDate.Value))
        {
            return "inactive";
        }

        return "active";
    }

    public bool IsActive(DateOnly date) => GetStatus(date) == ProjectStatus.Active;

    public void Archive(DateTime archivedAt)
    {
        if (IsArchived())
        {
            return;
        }

        ArchivedAt = archivedAt;
        UpdatedAt = archivedAt;
    }

    public void Unarchive(DateTime updatedAt)
    {
        if (!IsArchived())
        {
            return;
        }

        ArchivedAt = null;
        UpdatedAt = updatedAt;
    }
}