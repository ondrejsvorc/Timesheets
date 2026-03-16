using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Timesheets.Api.Projects.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Projects;

public class UpdateProjectTests : BaseIntegrationTest
{
    public UpdateProjectTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task UpdateProject_WithNonExistentId_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateProject.Request(
            "Updated Name",
            "REG-UPD-001",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(10)
        );

        var response = await Client.PutAsJsonAsync($"/api/projects/{nonExistentId}", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProject_WithInvalidData_ReturnsBadRequest()
    {
        // First create a valid project
        var createRequest = new CreateProject.Request(
            "Valid Project for Update",
            "REG-UPD-002",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(10)
        );
        var postResponse = await Client.PostAsJsonAsync("/api/projects", createRequest);
        var createdProject = await postResponse.Content.ReadFromJsonAsync<CreateProject.Response>();
        var projectId = createdProject!.Project.Id;

        // Then try to update it with invalid data (empty name)
        var invalidUpdateRequest = new UpdateProject.Request(
            "",
            "REG-UPD-002",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(10)
        );

        var putResponse = await Client.PutAsJsonAsync($"/api/projects/{projectId}", invalidUpdateRequest);
        Assert.Equal(HttpStatusCode.BadRequest, putResponse.StatusCode);
    }
}
