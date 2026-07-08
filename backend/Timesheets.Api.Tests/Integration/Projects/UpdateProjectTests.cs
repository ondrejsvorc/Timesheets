using System.Net;
using System.Net.Http.Json;
using Timesheets.Api.Features.Projects.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Projects;

public class UpdateProjectTests : BaseIntegrationTest
{
    public UpdateProjectTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task UpdateProject_WithNonExistentId_ReturnsNotFound()
    {
        Guid nonExistentId = Guid.CreateVersion7();
        UpdateProject.Request request = new("Updated Name", TestIdentifiers.Project(1020), DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10));
        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/projects/{nonExistentId}", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProject_WithInvalidData_ReturnsBadRequest()
    {
        string registrationNumber = TestIdentifiers.Project(1021);
        CreateProject.Request createRequest = new("Valid Project for Update", registrationNumber, DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10));
        HttpResponseMessage postResponse = await Client.PostAsJsonAsync("/api/projects", createRequest);
        CreateProject.Response? createdProject = await postResponse.Content.ReadFromJsonAsync<CreateProject.Response>();
        Guid projectId = createdProject!.Project.Id;

        HttpResponseMessage emptyNameResponse = await Client.PutAsJsonAsync($"/api/projects/{projectId}", new UpdateProject.Request("", registrationNumber, DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10)));
        Assert.Equal(HttpStatusCode.BadRequest, emptyNameResponse.StatusCode);

        HttpResponseMessage longNameResponse = await Client.PutAsJsonAsync($"/api/projects/{projectId}", new UpdateProject.Request(new string('a', 201), registrationNumber, DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10)));
        Assert.Equal(HttpStatusCode.BadRequest, longNameResponse.StatusCode);

        HttpResponseMessage longRegResponse = await Client.PutAsJsonAsync($"/api/projects/{projectId}", new UpdateProject.Request("Valid Name", new string('b', 101), DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10)));
        Assert.Equal(HttpStatusCode.BadRequest, longRegResponse.StatusCode);

        HttpResponseMessage invalidDatesResponse = await Client.PutAsJsonAsync($"/api/projects/{projectId}", new UpdateProject.Request("Valid Name", registrationNumber, DateTime.UtcNow.Date.AddDays(10), DateTime.UtcNow.Date));
        Assert.Equal(HttpStatusCode.BadRequest, invalidDatesResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateProject_WithValidData_ReturnsUpdatedProject()
    {
        CreateProject.Request createRequest = new("Project To Update", TestIdentifiers.Project(1022), DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10));
        HttpResponseMessage postResponse = await Client.PostAsJsonAsync("/api/projects", createRequest);
        CreateProject.Response? createdProject = await postResponse.Content.ReadFromJsonAsync<CreateProject.Response>();
        Guid projectId = createdProject!.Project.Id;

        UpdateProject.Request updateRequest = new("Updated Project Name", TestIdentifiers.Project(1023), createRequest.StartDate, createRequest.EndDate);
        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/projects/{projectId}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        UpdateProject.Response? updated = await response.Content.ReadFromJsonAsync<UpdateProject.Response>();
        Assert.NotNull(updated);
        Assert.Equal(updateRequest.Name, updated!.Project.Name);
        Assert.Equal(updateRequest.RegistrationNumber, updated.Project.RegistrationNumber);
        Assert.Equal(projectId, updated.Project.Id);
        Assert.Equal(0, updated.Project.ContractCount);
    }

    [Fact]
    public async Task UpdateProject_OutsideExistingAssignment_ReturnsBadRequest()
    {
        DateTime start = new(2032, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime assignmentEnd = start.AddMonths(2);
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, start, assignmentEnd);
        UpdateProject.Request request = new("Shortened Project", TestIdentifiers.Project(1024), start, assignmentEnd.AddDays(-1));

        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/projects/{setup.ProjectId}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
