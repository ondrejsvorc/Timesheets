using System.Security.Claims;
using Timesheets.Api.Employees;

namespace Timesheets.Api.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
    private const string AdminStudentPersonalNumber = "ST101971";
    private const string ScopedAffiliationClaimType = "eduPersonScopedAffiliation";

    public static bool IsAuthenticated(this ClaimsPrincipal principal) => principal.Identity?.IsAuthenticated == true;

    public static string GetFullName(this ClaimsPrincipal principal)
    {
        string? name =
            principal.FindFirstValue("displayName") ??
            principal.FindFirstValue("name") ??
            principal.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrWhiteSpace(name))
        {
            string? given = principal.FindFirstValue("given_name");
            string? family = principal.FindFirstValue("family_name");
            name = $"{given} {family}".Trim();
        }

        return string.IsNullOrWhiteSpace(name) ? "Unknown" : name;
    }

    public static string GetPersonalNumber(this ClaimsPrincipal principal)
    {
        string? raw = principal.FindFirstValue("personalNumber") ?? principal.FindFirstValue("personal_number");
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException("User personalNumber claim is missing.");
        }

        return raw.Trim();
    }

    public static string? GetTitleBefore(this ClaimsPrincipal principal) => principal.FindFirstValue("titleBefore");
    public static string? GetTitleAfter(this ClaimsPrincipal principal) => principal.FindFirstValue("titleAfter");

    public static bool CanUseTimesheets(this ClaimsPrincipal principal) =>
        principal.HasAffiliation("employee") || principal.IsAdminStudentException();

    public static Guid GetEmployeeTypeId(this ClaimsPrincipal principal) =>
        principal.HasAffiliation("faculty") ? EmployeeTypes.AcademicId : EmployeeTypes.NonAcademicId;

    private static bool IsAdminStudentException(this ClaimsPrincipal principal)
    {
        string? personalNumber = principal.FindFirstValue("personalNumber") ?? principal.FindFirstValue("personal_number");
        return string.Equals(personalNumber?.Trim(), AdminStudentPersonalNumber, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasAffiliation(this ClaimsPrincipal principal, string value) =>
        principal.GetScopedAffiliations().Any(affiliation =>
            string.Equals(affiliation, value, StringComparison.OrdinalIgnoreCase)
            || affiliation.StartsWith(value + "@", StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<string> GetScopedAffiliations(this ClaimsPrincipal principal) =>
        principal.FindAll(ScopedAffiliationClaimType)
            .SelectMany(claim => claim.Value.Split([' ', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
