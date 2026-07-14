using System.Security.Claims;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Features.Employees;

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

    [Fact]
    public void CanUseTimesheets_allows_employee_affiliation()
    {
        ClaimsPrincipal principal = Principal(
            new Claim("personalNumber", "123"),
            new Claim("eduPersonScopedAffiliation", "staff@ujep.cz member@ujep.cz employee@ujep.cz"));

        Assert.True(principal.CanUseTimesheets());
    }

    [Fact]
    public void CanUseTimesheets_rejects_student_without_exception()
    {
        ClaimsPrincipal principal = Principal(
            new Claim("personalNumber", "ST123"),
            new Claim("eduPersonScopedAffiliation", "student@ujep.cz member@ujep.cz"));

        Assert.False(principal.CanUseTimesheets());
    }

    [Fact]
    public void CanUseTimesheets_allows_admin_student_exception()
    {
        ClaimsPrincipal principal = Principal(
            new Claim("personalNumber", "ST101971"),
            new Claim("eduPersonScopedAffiliation", "student@ujep.cz member@ujep.cz"));

        Assert.True(principal.CanUseTimesheets());
    }

    [Fact]
    public void GetEmployeeTypeId_maps_faculty_to_academic_and_staff_to_nonacademic()
    {
        ClaimsPrincipal academic = Principal(new Claim("eduPersonScopedAffiliation", "faculty@ujep.cz employee@ujep.cz"));
        ClaimsPrincipal staff = Principal(new Claim("eduPersonScopedAffiliation", "staff@ujep.cz employee@ujep.cz"));

        Assert.Equal(EmployeeTypes.AcademicId, academic.GetEmployeeTypeId());
        Assert.Equal(EmployeeTypes.NonAcademicId, staff.GetEmployeeTypeId());
    }

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "oidc"));
}
