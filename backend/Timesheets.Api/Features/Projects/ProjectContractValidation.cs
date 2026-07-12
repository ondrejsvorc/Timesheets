using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;

namespace Timesheets.Api.Features.Projects;

internal static class ProjectContractValidation
{
    public const string DuplicateError = "Zakázka s tímto Id nebo názvem už v projektu existuje.";

    public static async Task<bool> HasDuplicateAsync(Guid contractEmployeeId, Guid? excludedContractId, string name, string registrationNumber, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        string trimmedName = name.Trim();
        string trimmedRegistrationNumber = registrationNumber.Trim();

        return await dbContext.Contracts
            .AsNoTracking()
            .Where(contract => contract.ProjectId == contractEmployeeId && (!excludedContractId.HasValue || contract.Id != excludedContractId.Value))
            .AnyAsync(contract => contract.Name == trimmedName || contract.RegistrationNumber == trimmedRegistrationNumber, cancellationToken);
    }
}
