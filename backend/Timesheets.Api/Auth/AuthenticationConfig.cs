using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace Timesheets.Api.Auth;

public static class AuthenticationConfig
{
    public const string SectionName = "Authentication";

    public static bool IsEnabled(IConfiguration configuration) => configuration.GetSection(SectionName).GetValue("Enabled", true);

    public static ClaimsPrincipal CreateDevPrincipal(IConfiguration configuration)
    {
        IConfigurationSection dev = configuration.GetSection($"{SectionName}:DevUser");

        static string Required(IConfigurationSection section, string key)
        {
            string? value = section[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Missing required configuration value '{section.Path}:{key}'.");
            }

            return value;
        }

        string email = Required(dev, "Email");
        string fullName = Required(dev, "FullName");
        string personalNumber = Required(dev, "PersonalNumber");
        string? titleBefore = dev.GetValue<string?>("TitleBefore", null);
        string? titleAfter = dev.GetValue<string?>("TitleAfter", null);

        List<Claim> claims = new()
        {
            new("eduPersonPrincipalName", email),
            new("displayName", fullName),
            new("personalNumber", personalNumber)
        };

        if (!string.IsNullOrWhiteSpace(titleBefore))
        {
            claims.Add(new Claim("titleBefore", titleBefore));
        }

        if (!string.IsNullOrWhiteSpace(titleAfter))
        {
            claims.Add(new Claim("titleAfter", titleAfter));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Dev"));
    }
}

