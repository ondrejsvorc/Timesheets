using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects;

internal static class ProjectContractValidation
{
    public const string DuplicateError = "Zakázka se stejným názvem nebo číslem už v projektu existuje.";

    public static Task<bool> HasDuplicateAsync(Guid projectId, Guid? excludedContractId, string name, string registrationNumber, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        string normalizedName = name.Trim().ToLowerInvariant();
        string normalizedRegistrationNumber = registrationNumber.Trim().ToLowerInvariant();
        return dbContext.Contracts.AsNoTracking().AnyAsync(contract =>
            contract.ProjectId == projectId
            && (!excludedContractId.HasValue || contract.Id != excludedContractId.Value)
            && (contract.Name.Trim().ToLower() == normalizedName || contract.RegistrationNumber.Trim().ToLower() == normalizedRegistrationNumber),
            cancellationToken);
    }
}
