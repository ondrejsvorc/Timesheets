using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects;

internal static class ProjectContractValidation
{
    public const string DuplicateError = "Zakázka s tímto Id nebo názvem už v projektu existuje.";

    private static readonly Regex MultiWhitespace = new(@"\s+", RegexOptions.Compiled);

    private static string NormalizeName(string value) => MultiWhitespace.Replace(value.Trim(), " ").ToLowerInvariant();

    // IDs are identifiers: treat any whitespace (incl. NBSP) as insignificant.
    private static string NormalizeRegistrationNumber(string value) => MultiWhitespace.Replace(value, "").Trim().ToLowerInvariant();

    public static Task<bool> HasDuplicateAsync(Guid projectId, Guid? excludedContractId, string name, string registrationNumber, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        string normalizedName = NormalizeName(name);
        string normalizedRegistrationNumber = NormalizeRegistrationNumber(registrationNumber);
        return dbContext.Contracts.AsNoTracking().AnyAsync(contract =>
            contract.ProjectId == projectId
            && (!excludedContractId.HasValue || contract.Id != excludedContractId.Value)
            && (NormalizeName(contract.Name) == normalizedName || NormalizeRegistrationNumber(contract.RegistrationNumber) == normalizedRegistrationNumber),
            cancellationToken);
    }
}
