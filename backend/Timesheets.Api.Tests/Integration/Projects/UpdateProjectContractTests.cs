using System.Net;
using System.Net.Http.Json;
using Timesheets.Api.Features.Projects.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Projects;

public class UpdateProjectContractTests : BaseIntegrationTest
{
    public UpdateProjectContractTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task UpdateProjectContract_WithInvalidData_ReturnsBadRequest()
    {
        CreateProject.Request createProjectRequest = new("Test Project For Contract Update", TestIdentifiers.Project(200), DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30));
        HttpResponseMessage postProjectResponse = await Client.PostAsJsonAsync("/api/projects", createProjectRequest);
        CreateProject.Response? createdProject = await postProjectResponse.Content.ReadFromJsonAsync<CreateProject.Response>();
        Guid contractEmployeeId = createdProject!.Project.Id;

        CreateProjectContract.Request createContractRequest = new("Valid Contract for Update", TestIdentifiers.Contract(200));
        HttpResponseMessage postContractResponse = await Client.PostAsJsonAsync($"/api/projects/{contractEmployeeId}/contracts", createContractRequest);
        CreateProjectContract.Response? createdContract = await postContractResponse.Content.ReadFromJsonAsync<CreateProjectContract.Response>();
        Guid contractId = createdContract!.ProjectContract.Id;

        HttpResponseMessage emptyNameResponse = await Client.PutAsJsonAsync($"/api/projects/{contractEmployeeId}/contracts/{contractId}", new UpdateProjectContract.Request("", TestIdentifiers.Contract(201)));
        Assert.Equal(HttpStatusCode.BadRequest, emptyNameResponse.StatusCode);

        HttpResponseMessage longNameResponse = await Client.PutAsJsonAsync($"/api/projects/{contractEmployeeId}/contracts/{contractId}", new UpdateProjectContract.Request(new string('a', 201), TestIdentifiers.Contract(201)));
        Assert.Equal(HttpStatusCode.BadRequest, longNameResponse.StatusCode);

        HttpResponseMessage longRegResponse = await Client.PutAsJsonAsync($"/api/projects/{contractEmployeeId}/contracts/{contractId}", new UpdateProjectContract.Request("Valid Name", new string('b', 101)));
        Assert.Equal(HttpStatusCode.BadRequest, longRegResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateProjectContract_WithValidData_ReturnsUpdatedContract()
    {
        CreateProject.Request createProjectRequest = new("Project For Contract Update Response", TestIdentifiers.Project(201), DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30));
        HttpResponseMessage postProjectResponse = await Client.PostAsJsonAsync("/api/projects", createProjectRequest);
        CreateProject.Response? createdProject = await postProjectResponse.Content.ReadFromJsonAsync<CreateProject.Response>();
        Guid contractEmployeeId = createdProject!.Project.Id;

        CreateProjectContract.Request createContractRequest = new("Contract Before Update", TestIdentifiers.Contract(202));
        HttpResponseMessage postContractResponse = await Client.PostAsJsonAsync($"/api/projects/{contractEmployeeId}/contracts", createContractRequest);
        CreateProjectContract.Response? createdContract = await postContractResponse.Content.ReadFromJsonAsync<CreateProjectContract.Response>();
        Guid contractId = createdContract!.ProjectContract.Id;

        UpdateProjectContract.Request updateRequest = new("Contract After Update", "54321 10 9876 54");
        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/projects/{contractEmployeeId}/contracts/{contractId}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        UpdateProjectContract.Response? updated = await response.Content.ReadFromJsonAsync<UpdateProjectContract.Response>();
        Assert.NotNull(updated);
        Assert.Equal(updateRequest.Name, updated!.ProjectContract.Name);
        Assert.Equal(updateRequest.RegistrationNumber, updated.ProjectContract.RegistrationNumber);
        Assert.Equal(contractId, updated.ProjectContract.Id);
        Assert.Equal(0, updated.ProjectContract.EmployeeCount);
    }

    [Fact]
    public async Task UpdateProjectContract_WithNormalizedDuplicate_ReturnsBadRequest()
    {
        string suffix = TestIdentifiers.Suffix();
        CreateProject.Request projectRequest = new($"Project {suffix}", TestIdentifiers.Project(202), DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30));
        CreateProject.Response? project = await (await Client.PostAsJsonAsync("/api/projects", projectRequest)).Content.ReadFromJsonAsync<CreateProject.Response>();
        Guid contractEmployeeId = project!.Project.Id;

        HttpResponseMessage firstResponse = await Client.PostAsJsonAsync($"/api/projects/{contractEmployeeId}/contracts", new CreateProjectContract.Request("First Contract", TestIdentifiers.Contract(203)));
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        CreateProjectContract.Response? second = await (await Client.PostAsJsonAsync($"/api/projects/{contractEmployeeId}/contracts", new CreateProjectContract.Request("Second Contract", TestIdentifiers.Contract(204)))).Content.ReadFromJsonAsync<CreateProjectContract.Response>();

        HttpResponseMessage duplicateName = await Client.PutAsJsonAsync($"/api/projects/{contractEmployeeId}/contracts/{second!.ProjectContract.Id}", new UpdateProjectContract.Request("  first contract  ", TestIdentifiers.Contract(204)));
        HttpResponseMessage duplicateRegistrationNumber = await Client.PutAsJsonAsync($"/api/projects/{contractEmployeeId}/contracts/{second.ProjectContract.Id}", new UpdateProjectContract.Request("Second Contract", TestIdentifiers.Contract(203)));

        Assert.Equal(HttpStatusCode.BadRequest, duplicateName.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateRegistrationNumber.StatusCode);
    }
}
