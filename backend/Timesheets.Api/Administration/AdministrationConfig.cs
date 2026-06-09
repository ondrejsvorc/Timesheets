namespace Timesheets.Api.Administration;

public sealed class AdministrationOptions
{
    public const string SectionName = "Administration";

    public string[] RoleManagerEmails { get; init; } = [];
}

public static class RoleManagerAuthorization
{
    public static bool IsRoleManager(string email, AdministrationOptions options) =>
        options.RoleManagerEmails.Any(roleManagerEmail =>
            string.Equals(roleManagerEmail.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase));
}
