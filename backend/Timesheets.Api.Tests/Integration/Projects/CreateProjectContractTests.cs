using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Timesheets.Api.Projects.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Projects;

public class CreateProjectContractTests : BaseIntegrationTest
{
    public CreateProjectContractTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<Guid> CreateProjectAsync()
    {
        var request = new CreateProject.Request(
            "Test Project For Contract",
            "REG-123",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(30)
        );
        var response = await Client.PostAsJsonAsync("/api/projects", request);
        var content = await response.Content.ReadFromJsonAsync<CreateProject.Response>();
        return content!.Project.Id;
    }

    [Fact]
    public async Task CreateProjectContract_WithValidData_ReturnsCreated()
    {
        var projectId = await CreateProjectAsync();
        var request = new CreateProjectContract.Request(
            "Test Contract",
            "CONT-001"
        );

        var response = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateProjectContract_WithEmptyName_ReturnsBadRequest()
    {
        var projectId = await CreateProjectAsync();
        var request = new CreateProjectContract.Request(
            "",
            "CONT-002"
        );

        var response = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProjectContract_WithLongName_ReturnsBadRequest()
    {
        var projectId = await CreateProjectAsync();
        var request = new CreateProjectContract.Request(
            new string('A', 201),
            "CONT-003"
        );

        var response = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProjectContract_WithLongRegistrationNumber_ReturnsBadRequest()
    {
        var projectId = await CreateProjectAsync();
        var request = new CreateProjectContract.Request(
            "Valid Name",
            new string('B', 101)
        );

        var response = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
