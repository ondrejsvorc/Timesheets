namespace Timesheets.Api.Administration;

public sealed class AdministrationOptions
{
    public const string SectionName = "Administration";

    public string[] RoleManagerPersonalNumbers { get; init; } = [];
}

public static class RoleManagerAuthorization
{
    public static bool IsRoleManager(string personalNumber, AdministrationOptions options) =>
        options.RoleManagerPersonalNumbers.Any(roleManagerPersonalNumber =>
            string.Equals(roleManagerPersonalNumber.Trim(), personalNumber.Trim(), StringComparison.OrdinalIgnoreCase));
}
