using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace Timesheets.Api.Features.Auth;

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

        string firstName = Required(dev, "FirstName");
        string surname = Required(dev, "Surname");
        string personalNumber = Required(dev, "PersonalNumber");
        string? titleBefore = dev.GetValue<string?>("TitleBefore", null);
        string? titleAfter = dev.GetValue<string?>("TitleAfter", null);
        string[] affiliations = dev.GetSection("Affiliation").Get<string[]>() ?? [];

        List<Claim> claims =
        [
            new("given_name", firstName),
            new("family_name", surname),
            new("personalNumber", personalNumber)
        ];

        if (!string.IsNullOrWhiteSpace(titleBefore))
        {
            claims.Add(new Claim("titleBefore", titleBefore));
        }

        if (!string.IsNullOrWhiteSpace(titleAfter))
        {
            claims.Add(new Claim("titleAfter", titleAfter));
        }

        foreach (string affiliation in affiliations.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            claims.Add(new Claim("eduPersonScopedAffiliation", affiliation));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Dev"));
    }
}
