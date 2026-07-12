using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common;
using Timesheets.Api.Domain;

namespace Timesheets.Api.Features.Projects;

internal static class ProjectCatalogValidation
{
    public const string DuplicateError = "Projekt s tímto Id nebo názvem už existuje.";

    public static async Task<bool> HasDuplicateAsync(Guid? excludedProjectId, string name, string registrationNumber, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        string normalizedName = CatalogNormalization.NormalizeName(name);
        string normalizedRegistrationNumber = CatalogNormalization.NormalizeRegistrationNumber(registrationNumber);

        return await dbContext.Projects
            .AsNoTracking()
            .Where(project => !excludedProjectId.HasValue || project.Id != excludedProjectId.Value)
            .AnyAsync(
                project => project.NormalizedName == normalizedName || project.NormalizedRegistrationNumber == normalizedRegistrationNumber,
                cancellationToken);
    }
}
