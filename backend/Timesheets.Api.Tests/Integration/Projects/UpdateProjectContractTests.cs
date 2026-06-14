using System.Net;
using System.Net.Http.Json;
using Timesheets.Api.Projects.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Projects;

public class UpdateProjectContractTests : BaseIntegrationTest
{
    public UpdateProjectContractTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task UpdateProjectContract_WithInvalidData_ReturnsBadRequest()
    {
        CreateProject.Request createProjectRequest = new("Test Project For Contract Update", "REG-UPD-123", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30));
        HttpResponseMessage postProjectResponse = await Client.PostAsJsonAsync("/api/projects", createProjectRequest);
        CreateProject.Response? createdProject = await postProjectResponse.Content.ReadFromJsonAsync<CreateProject.Response>();
        Guid projectId = createdProject!.Project.Id;

        CreateProjectContract.Request createContractRequest = new("Valid Contract for Update", "CONT-UPD-001");
        HttpResponseMessage postContractResponse = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", createContractRequest);
        CreateProjectContract.Response? createdContract = await postContractResponse.Content.ReadFromJsonAsync<CreateProjectContract.Response>();
        Guid contractId = createdContract!.ProjectContract.Id;

        HttpResponseMessage emptyNameResponse = await Client.PutAsJsonAsync($"/api/projects/{projectId}/contracts/{contractId}", new UpdateProjectContract.Request("", "CONT-UPD-002"));
        Assert.Equal(HttpStatusCode.BadRequest, emptyNameResponse.StatusCode);

        HttpResponseMessage longNameResponse = await Client.PutAsJsonAsync($"/api/projects/{projectId}/contracts/{contractId}", new UpdateProjectContract.Request(new string('a', 201), "CONT-UPD-002"));
        Assert.Equal(HttpStatusCode.BadRequest, longNameResponse.StatusCode);

        HttpResponseMessage longRegResponse = await Client.PutAsJsonAsync($"/api/projects/{projectId}/contracts/{contractId}", new UpdateProjectContract.Request("Valid Name", new string('b', 101)));
        Assert.Equal(HttpStatusCode.BadRequest, longRegResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateProjectContract_WithValidData_ReturnsUpdatedContract()
    {
        CreateProject.Request createProjectRequest = new("Project For Contract Update Response", "REG-UPD-CON-001", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30));
        HttpResponseMessage postProjectResponse = await Client.PostAsJsonAsync("/api/projects", createProjectRequest);
        CreateProject.Response? createdProject = await postProjectResponse.Content.ReadFromJsonAsync<CreateProject.Response>();
        Guid projectId = createdProject!.Project.Id;

        CreateProjectContract.Request createContractRequest = new("Contract Before Update", "CONT-UPD-002");
        HttpResponseMessage postContractResponse = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", createContractRequest);
        CreateProjectContract.Response? createdContract = await postContractResponse.Content.ReadFromJsonAsync<CreateProjectContract.Response>();
        Guid contractId = createdContract!.ProjectContract.Id;

        UpdateProjectContract.Request updateRequest = new("Contract After Update", "CONT-UPD-003");
        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/projects/{projectId}/contracts/{contractId}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        UpdateProjectContract.Response? updated = await response.Content.ReadFromJsonAsync<UpdateProjectContract.Response>();
        Assert.NotNull(updated);
        Assert.Equal(updateRequest.Name, updated!.ProjectContract.Name);
        Assert.Equal(updateRequest.RegistrationNumber, updated.ProjectContract.RegistrationNumber);
        Assert.Equal(contractId, updated.ProjectContract.Id);
        Assert.Equal(0, updated.ProjectContract.EmployeeCount);
    }
}
