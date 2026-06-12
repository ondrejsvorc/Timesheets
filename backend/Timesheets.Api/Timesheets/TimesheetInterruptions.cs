namespace Timesheets.Api.Timesheets;

internal static class TimesheetInterruptions
{
    private static readonly HashSet<string> BusinessTripCodes =
    [
        "SCP", "SCS", "SCT", "SCZ", "SCZE", "SCZP", "SCZS"
    ];

    public static IReadOnlyList<string> ParseCodes(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(code => code.ToUpperInvariant())
            .Where(code => code.Length > 0)
            .ToArray();
    }

    public static bool HasBusinessTripInterruption(string? raw) =>
        ParseCodes(raw).Any(BusinessTripCodes.Contains);

    public static bool HasCoreOnlyInterruption(string? raw) =>
        ParseCodes(raw).Any(code => code is "M" || code.StartsWith('N'));

    /// <summary>
    /// Proporcionální přerušení (dovolená apod.) — STAG a bilance se nevyžadují.
    /// </summary>
    public static bool HasProportionalInterruption(string? raw)
    {
        IReadOnlyList<string> codes = ParseCodes(raw);
        if (codes.Count == 0)
        {
            return false;
        }

        if (codes.Any(BusinessTripCodes.Contains))
        {
            return false;
        }

        return !HasCoreOnlyInterruption(raw);
    }

    public static bool SkipAllocationRules(string? raw) =>
        HasBusinessTripInterruption(raw) || HasProportionalInterruption(raw);
}
