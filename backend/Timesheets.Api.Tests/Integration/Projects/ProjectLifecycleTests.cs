using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Timesheets.Api.Projects.Endpoints;
using Timesheets.Api.Tests.Integration;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Projects;

public class ProjectLifecycleTests : BaseIntegrationTest
{
    public ProjectLifecycleTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Project_Lifecycle_HappyPath_CompletesSuccessfully()
    {
        // Arrange
        var createRequest = new CreateProject.Request(
            "Test Project Lifecycle",
            "REG-LIFECYCLE-001",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(30)
        );

        // 1. POST /api/projects
        var postResponse = await Client.PostAsJsonAsync("/api/projects", createRequest);
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var createdProject = await postResponse.Content.ReadFromJsonAsync<CreateProject.Response>();
        Assert.NotNull(createdProject);
        var projectId = createdProject!.Project.Id;

        // 2. GET /api/projects/{id}
        var getResponse1 = await Client.GetAsync($"/api/projects/{projectId}");
        Assert.Equal(HttpStatusCode.OK, getResponse1.StatusCode);

        var projectData1 = await getResponse1.Content.ReadFromJsonAsync<GetProject.Response>();
        Assert.NotNull(projectData1);
        Assert.Equal(createRequest.Name, projectData1!.Project.Name);
        Assert.Equal(createRequest.RegistrationNumber, projectData1.Project.RegistrationNumber);

        // 3. PUT /api/projects/{id}
        var updateRequest = new UpdateProject.Request(
            "Updated Lifecycle Project",
            "REG-LIFECYCLE-002",
            createRequest.StartDate,
            createRequest.EndDate
        );
        var putResponse = await Client.PutAsJsonAsync($"/api/projects/{projectId}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var updatedProject = await putResponse.Content.ReadFromJsonAsync<UpdateProject.Response>();
        Assert.NotNull(updatedProject);
        Assert.Equal(updateRequest.Name, updatedProject!.Project.Name);
        Assert.Equal(updateRequest.RegistrationNumber, updatedProject.Project.RegistrationNumber);

        // 4. GET /api/projects/{id}
        var getResponse2 = await Client.GetAsync($"/api/projects/{projectId}");
        Assert.Equal(HttpStatusCode.OK, getResponse2.StatusCode);

        var projectData2 = await getResponse2.Content.ReadFromJsonAsync<GetProject.Response>();
        Assert.NotNull(projectData2);
        Assert.Equal(updateRequest.Name, projectData2!.Project.Name);
        Assert.Equal(updateRequest.RegistrationNumber, projectData2.Project.RegistrationNumber);

        // 5. DELETE /api/projects/{id}
        var deleteResponse = await Client.DeleteAsync($"/api/projects/{projectId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // 6. GET /api/projects/{id}
        var getResponse3 = await Client.GetAsync($"/api/projects/{projectId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse3.StatusCode);
    }
}
