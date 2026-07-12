using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;

namespace Timesheets.Api.Features.Projects;

internal static class ProjectCatalogValidation
{
    public const string DuplicateError = "Projekt s tímto Id nebo názvem už existuje.";

    public static async Task<bool> HasDuplicateAsync(Guid? excludedProjectId, string name, string registrationNumber, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        string trimmedName = name.Trim();
        string trimmedRegistrationNumber = registrationNumber.Trim();

        return await dbContext.Projects
            .AsNoTracking()
            .Where(project => !excludedProjectId.HasValue || project.Id != excludedProjectId.Value)
            .AnyAsync(
                project => project.Name == trimmedName || project.RegistrationNumber == trimmedRegistrationNumber,
                cancellationToken);
    }
}
