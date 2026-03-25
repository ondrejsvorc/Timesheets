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

        // 1. Empty Name
        var emptyNameRequest = new UpdateProject.Request(
            "",
            "REG-UPD-002",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(10)
        );
        var response1 = await Client.PutAsJsonAsync($"/api/projects/{projectId}", emptyNameRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response1.StatusCode);

        // 2. Name too long
        var longNameRequest = new UpdateProject.Request(
            new string('a', 201),
            "REG-UPD-002",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(10)
        );
        var response2 = await Client.PutAsJsonAsync($"/api/projects/{projectId}", longNameRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);

        // 3. RegistrationNumber too long
        var longRegRequest = new UpdateProject.Request(
            "Valid Name",
            new string('b', 101),
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(10)
        );
        var response3 = await Client.PutAsJsonAsync($"/api/projects/{projectId}", longRegRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response3.StatusCode);

        // 4. StartDate >= EndDate
        var invalidDatesRequest = new UpdateProject.Request(
            "Valid Name",
            "REG-UPD-002",
            DateTime.UtcNow.Date.AddDays(10),
            DateTime.UtcNow.Date
        );
        var response4 = await Client.PutAsJsonAsync($"/api/projects/{projectId}", invalidDatesRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response4.StatusCode);
    }
}
