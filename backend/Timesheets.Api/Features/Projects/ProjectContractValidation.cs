using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;

namespace Timesheets.Api.Features.Projects;

internal static class ProjectContractValidation
{
    public const string DuplicateError = "Zakázka s tímto Id nebo názvem už v projektu existuje.";

    private static readonly Regex MultiWhitespace = new(@"\s+", RegexOptions.Compiled);

    private static string NormalizeName(string value) => MultiWhitespace.Replace(value.Trim(), " ").ToLowerInvariant();
    private static string NormalizeRegistrationNumber(string value) => MultiWhitespace.Replace(value, "").Trim().ToLowerInvariant();

    public static async Task<bool> HasDuplicateAsync(Guid contractEmployeeId, Guid? excludedContractId, string name, string registrationNumber, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        string normalizedName = NormalizeName(name);
        string normalizedRegistrationNumber = NormalizeRegistrationNumber(registrationNumber);

        var contracts = await dbContext.Contracts
            .AsNoTracking()
            .Where(contract => contract.ProjectId == contractEmployeeId && (!excludedContractId.HasValue || contract.Id != excludedContractId.Value))
            .Select(contract => new { contract.Name, contract.RegistrationNumber })
            .ToListAsync(cancellationToken);

        return contracts.Any(contract => NormalizeName(contract.Name) == normalizedName || NormalizeRegistrationNumber(contract.RegistrationNumber) == normalizedRegistrationNumber);
    }
}
