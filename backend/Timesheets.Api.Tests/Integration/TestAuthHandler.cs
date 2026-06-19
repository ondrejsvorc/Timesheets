using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Timesheets.Api.Tests.Integration;

public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string PersonalNumber = "test-auth";
    public const string PersonalNumberHeader = "X-Test-PersonalNumber";
    public const string DisplayNameHeader = "X-Test-DisplayName";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string personalNumber = Request.Headers.TryGetValue(PersonalNumberHeader, out var personalNumbers)
            ? personalNumbers.ToString()
            : PersonalNumber;
        string displayName = Request.Headers.TryGetValue(DisplayNameHeader, out var displayNames)
            ? displayNames.ToString()
            : "Test User";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, personalNumber),
            new Claim("displayName", displayName),
            new Claim("personalNumber", personalNumber)
        };
        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
