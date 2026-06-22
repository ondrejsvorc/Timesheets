using System.Net;
using System.Net.Http.Json;
using Timesheets.Api.Projects.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Projects;

public class ProjectLifecycleTests : BaseIntegrationTest
{
    public ProjectLifecycleTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Project_Lifecycle_HappyPath_CompletesSuccessfully()
    {
        CreateProject.Request createRequest = new("Test Project Lifecycle", TestIdentifiers.Project(1001), DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30));
        HttpResponseMessage postResponse = await Client.PostAsJsonAsync("/api/projects", createRequest);
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        CreateProject.Response? createdProject = await postResponse.Content.ReadFromJsonAsync<CreateProject.Response>();
        Guid projectId = createdProject!.Project.Id;

        HttpResponseMessage getResponse = await Client.GetAsync($"/api/projects/{projectId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        GetProject.Response? projectData = await getResponse.Content.ReadFromJsonAsync<GetProject.Response>();
        Assert.NotNull(projectData);
        Assert.Equal(createRequest.Name, projectData!.Project.Name);
        Assert.Equal(createRequest.RegistrationNumber, projectData.Project.RegistrationNumber);

        UpdateProject.Request updateRequest = new("Updated Lifecycle Project", TestIdentifiers.Project(1002), createRequest.StartDate, createRequest.EndDate);
        HttpResponseMessage putResponse = await Client.PutAsJsonAsync($"/api/projects/{projectId}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        UpdateProject.Response? updatedProject = await putResponse.Content.ReadFromJsonAsync<UpdateProject.Response>();
        Assert.NotNull(updatedProject);
        Assert.Equal(updateRequest.Name, updatedProject!.Project.Name);
        Assert.Equal(updateRequest.RegistrationNumber, updatedProject.Project.RegistrationNumber);

        HttpResponseMessage deleteResponse = await Client.DeleteAsync($"/api/projects/{projectId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        HttpResponseMessage getDeletedResponse = await Client.GetAsync($"/api/projects/{projectId}");
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }
}
