namespace Timesheets.Api.Common.Extensions;

using System.Security.Claims;

public static class ClaimsPrincipalExtensions
{
    private static readonly string[] EmailClaimTypes =
    [
        "email",
        ClaimTypes.Email,
        "mail",
        "upn",
        "preferred_username",
    ];

    public static bool IsAuthenticated(this ClaimsPrincipal principal) => principal.Identity?.IsAuthenticated == true;

    public static string GetEmail(this ClaimsPrincipal principal)
    {
        foreach (string claimType in EmailClaimTypes)
        {
            string? value = principal.FindFirstValue(claimType)?.Trim();
            if (!string.IsNullOrWhiteSpace(value) && value.Contains('@', StringComparison.Ordinal))
            {
                return value;
            }
        }

        foreach (string claimType in EmailClaimTypes)
        {
            string? value = principal.FindFirstValue(claimType)?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        throw new InvalidOperationException("User email claim is missing.");
    }

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
}
