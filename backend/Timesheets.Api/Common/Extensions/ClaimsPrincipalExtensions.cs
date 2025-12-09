namespace Timesheets.Api.Common.Extensions;

using System.Security.Claims;

public static class ClaimsPrincipalExtensions
{
    public static bool IsAuthenticated(this ClaimsPrincipal principal) => principal.Identity?.IsAuthenticated == true;

    public static string GetEmail(this ClaimsPrincipal principal) => principal.FindFirstValue("email") ?? string.Empty;

    public static string GetFullName(this ClaimsPrincipal principal) => principal.FindFirstValue("displayName") ?? string.Empty;

    public static int? GetPersonalNumber(this ClaimsPrincipal principal)
    {
        string? schac = principal.FindFirst("schacPersonalUniqueCode")?.Value;
        if (string.IsNullOrWhiteSpace(schac))
        {
            return null;
        }
        ReadOnlySpan<char> last = schac.AsSpan(schac.LastIndexOf(':') + 1);
        return int.TryParse(last, out int value) ? value : null;
    }
}
