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
        ClaimsPrincipal principal = Principal(
            new Claim(claimType, " employee-42 "),
            new Claim("email", "st101971@ujep.cz"));

        Assert.Equal("employee-42", principal.GetPersonalNumber());
    }

    [Fact]
    public void GetPersonalNumber_requires_explicit_claim()
    {
        ClaimsPrincipal principal = Principal(new Claim("email", "st101971@ujep.cz"));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(principal.GetPersonalNumber);

        Assert.Contains("personalNumber", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetEmail_uses_eduPersonPrincipalName()
    {
        ClaimsPrincipal principal = Principal(
            new Claim("email", "ignored@students.ujep.cz"),
            new Claim("eduPersonPrincipalName", " st101971@ujep.cz "));

        Assert.Equal("st101971@ujep.cz", principal.GetEmail());
    }

    [Fact]
    public void GetEmail_requires_eduPersonPrincipalName()
    {
        ClaimsPrincipal principal = Principal(new Claim("email", "ignored@students.ujep.cz"));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(principal.GetEmail);

        Assert.Contains("eduPersonPrincipalName", exception.Message, StringComparison.Ordinal);
    }

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "oidc"));
}
