using System.Text.RegularExpressions;

namespace Timesheets.Api.Common;

public static class CatalogNormalization
{
    private static readonly Regex MultiWhitespace = new(@"\s+", RegexOptions.Compiled);

    public static string NormalizeName(string value) =>
        MultiWhitespace.Replace(value.Trim(), " ").ToLowerInvariant();

    public static string NormalizeRegistrationNumber(string value) =>
        MultiWhitespace.Replace(value, string.Empty).Trim().ToLowerInvariant();
}
