using System.Security.Claims;
using Timesheets.Api.Common.Extensions;

namespace Timesheets.Api.Tests.Unit;

public sealed class ClaimsPrincipalExtensionsTests
{
    [Theory]
    [InlineData("personalNumber")]
    [InlineData("personal_number")]
    public void GetPersonalNumber_reads_supported_claim_names(string claimType)
    {
        ClaimsPrincipal principal = Principal(new Claim(claimType, " employee-42 "));

        Assert.Equal("employee-42", principal.GetPersonalNumber());
    }

    [Fact]
    public void GetPersonalNumber_requires_explicit_claim()
    {
        ClaimsPrincipal principal = Principal(new Claim("displayName", "Test User"));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(principal.GetPersonalNumber);

        Assert.Contains("personalNumber", exception.Message, StringComparison.Ordinal);
    }

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "oidc"));
}
