using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Timesheets.Api.Projects.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Projects;

public class CreateProjectTests : BaseIntegrationTest
{
    public CreateProjectTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateProject_WithValidData_ReturnsCreated()
    {
        var request = new CreateProject.Request(
            "Isolated Create Test",
            "REG-CREATE-001",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(10)
        );

        var response = await Client.PostAsJsonAsync("/api/projects", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithEmptyName_ReturnsBadRequest()
    {
        var request = new CreateProject.Request(
            "",
            "REG-CREATE-002",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(10)
        );

        var response = await Client.PostAsJsonAsync("/api/projects", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithLongName_ReturnsBadRequest()
    {
        var request = new CreateProject.Request(
            new string('A', 201),
            "REG-CREATE-003",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(10)
        );

        var response = await Client.PostAsJsonAsync("/api/projects", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithLongRegistrationNumber_ReturnsBadRequest()
    {
        var request = new CreateProject.Request(
            "Valid Name",
            new string('B', 101),
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(10)
        );

        var response = await Client.PostAsJsonAsync("/api/projects", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithStartDateEqualEndDate_ReturnsBadRequest()
    {
        var date = DateTime.UtcNow.Date;
        var request = new CreateProject.Request(
            "Valid Name",
            "REG-CREATE-004",
            date,
            date
        );

        var response = await Client.PostAsJsonAsync("/api/projects", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithStartDateGreaterEndDate_ReturnsBadRequest()
    {
        var date = DateTime.UtcNow.Date;
        var request = new CreateProject.Request(
            "Valid Name",
            "REG-CREATE-005",
            date.AddDays(1),
            date
        );

        var response = await Client.PostAsJsonAsync("/api/projects", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithNullEndDate_ReturnsCreated()
    {
        var request = new CreateProject.Request(
            "Valid Name",
            "REG-CREATE-006",
            DateTime.UtcNow.Date,
            null
        );

        var response = await Client.PostAsJsonAsync("/api/projects", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithUtcMidnightJsonString_ReturnsCreated()
    {
        using var content = new StringContent(
            """{"name":"Utc Midnight Project","registrationNumber":"REG-CREATE-007","startDate":"2026-01-01T00:00:00.000Z","endDate":null}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await Client.PostAsync("/api/projects", content);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
