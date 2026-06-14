using System.Net;
using System.Net.Http.Json;
using Timesheets.Api.Projects.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Projects;

public class CreateProjectContractTests : BaseIntegrationTest
{
    public CreateProjectContractTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<Guid> CreateProjectAsync()
    {
        CreateProject.Request request = new("Test Project For Contract", "REG-123", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30));
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/projects", request);
        CreateProject.Response? content = await response.Content.ReadFromJsonAsync<CreateProject.Response>();
        return content!.Project.Id;
    }

    [Fact]
    public async Task CreateProjectContract_WithValidData_ReturnsCreated()
    {
        Guid projectId = await CreateProjectAsync();
        CreateProjectContract.Request request = new("Test Contract", "CONT-001");
        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateProjectContract_WithEmptyName_ReturnsBadRequest()
    {
        Guid projectId = await CreateProjectAsync();
        CreateProjectContract.Request request = new("", "CONT-002");
        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProjectContract_WithLongName_ReturnsBadRequest()
    {
        Guid projectId = await CreateProjectAsync();
        CreateProjectContract.Request request = new(new string('A', 201), "CONT-003");
        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProjectContract_WithLongRegistrationNumber_ReturnsBadRequest()
    {
        Guid projectId = await CreateProjectAsync();
        CreateProjectContract.Request request = new("Valid Name", new string('B', 101));
        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
