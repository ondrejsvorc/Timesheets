using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Timesheets.Api.Projects.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Projects;

public class UpdateProjectContractTests : BaseIntegrationTest
{
    public UpdateProjectContractTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task UpdateProjectContract_WithInvalidData_ReturnsBadRequest()
    {
        // 1. Create Project
        var createProjectRequest = new CreateProject.Request(
            "Test Project For Contract Update",
            "REG-UPD-123",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(30)
        );
        var postProjectResponse = await Client.PostAsJsonAsync("/api/projects", createProjectRequest);
        var createdProject = await postProjectResponse.Content.ReadFromJsonAsync<CreateProject.Response>();
        var projectId = createdProject!.Project.Id;

        // 2. Create Contract
        var createContractRequest = new CreateProjectContract.Request(
            "Valid Contract for Update",
            "CONT-UPD-001"
        );
        var postContractResponse = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", createContractRequest);
        var createdContract = await postContractResponse.Content.ReadFromJsonAsync<CreateProjectContract.Response>();
        var contractId = createdContract!.ProjectContract.Id;

        // 3. Update tests
        var emptyNameRequest = new UpdateProjectContract.Request("", "CONT-UPD-002");
        var response1 = await Client.PutAsJsonAsync($"/api/projects/{projectId}/contracts/{contractId}", emptyNameRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response1.StatusCode);

        var longNameRequest = new UpdateProjectContract.Request(new string('a', 201), "CONT-UPD-002");
        var response2 = await Client.PutAsJsonAsync($"/api/projects/{projectId}/contracts/{contractId}", longNameRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);

        var longRegRequest = new UpdateProjectContract.Request("Valid Name", new string('b', 101));
        var response3 = await Client.PutAsJsonAsync($"/api/projects/{projectId}/contracts/{contractId}", longRegRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response3.StatusCode);
    }

    [Fact]
    public async Task UpdateProjectContract_WithValidData_ReturnsUpdatedContract()
    {
        var createProjectRequest = new CreateProject.Request(
            "Project For Contract Update Response",
            "REG-UPD-CON-001",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(30));
        var postProjectResponse = await Client.PostAsJsonAsync("/api/projects", createProjectRequest);
        var createdProject = await postProjectResponse.Content.ReadFromJsonAsync<CreateProject.Response>();
        var projectId = createdProject!.Project.Id;

        var createContractRequest = new CreateProjectContract.Request(
            "Contract Before Update",
            "CONT-UPD-002");
        var postContractResponse = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", createContractRequest);
        var createdContract = await postContractResponse.Content.ReadFromJsonAsync<CreateProjectContract.Response>();
        var contractId = createdContract!.ProjectContract.Id;

        var updateRequest = new UpdateProjectContract.Request("Contract After Update", "CONT-UPD-003");
        var response = await Client.PutAsJsonAsync($"/api/projects/{projectId}/contracts/{contractId}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<UpdateProjectContract.Response>();
        Assert.NotNull(updated);
        Assert.Equal(updateRequest.Name, updated!.ProjectContract.Name);
        Assert.Equal(updateRequest.RegistrationNumber, updated.ProjectContract.RegistrationNumber);
        Assert.Equal(contractId, updated.ProjectContract.Id);
        Assert.Equal(0, updated.ProjectContract.EmployeeCount);
    }
}
