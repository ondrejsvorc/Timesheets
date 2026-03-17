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
}
