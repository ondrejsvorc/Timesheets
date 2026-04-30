namespace Timesheets.Api.Common.Extensions;

using System.Security.Claims;

public static class ClaimsPrincipalExtensions
{
    public static bool IsAuthenticated(this ClaimsPrincipal principal) => principal.Identity?.IsAuthenticated == true;

    public static string GetEmail(this ClaimsPrincipal principal)
    {
        // Depending on OIDC claim mapping, email can be stored as "email" or ClaimTypes.Email.
        string? email =
            principal.FindFirstValue("email") ??
            principal.FindFirstValue(ClaimTypes.Email) ??
            principal.FindFirstValue("mail") ??
            principal.FindFirstValue("upn") ??
            principal.FindFirstValue("preferred_username");

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("User email claim is missing.");
        }

        return email;
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

    public static int GetPersonalNumber(this ClaimsPrincipal principal)
    {
        string? raw = principal.FindFirstValue("personalNumber") ?? principal.FindFirstValue("personal_number");
        if (string.IsNullOrWhiteSpace(raw) || !int.TryParse(raw, out int personalNumber))
        {
            throw new InvalidOperationException("User personalNumber claim is missing or invalid.");
        }
        return personalNumber;
    }
}
