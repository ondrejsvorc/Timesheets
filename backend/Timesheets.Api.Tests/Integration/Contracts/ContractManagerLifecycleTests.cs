using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Timesheets.Api.Contracts.Endpoints;
using Timesheets.Api.Projects.Endpoints;
using Timesheets.Api.Tests.Integration;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Contracts;

public class ContractManagerLifecycleTests : BaseIntegrationTest
{
    public ContractManagerLifecycleTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Contract_Manager_Lifecycle_HappyPath_CompletesSuccessfully()
    {
        // 1. Setup: Create Project
        var createProjectRequest = new CreateProject.Request(
            "Test Project for Contract Managers",
            "REG-CON-MAN-001",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(30)
        );
        var projectResponse = await Client.PostAsJsonAsync("/api/projects", createProjectRequest);
        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);
        var createdProject = await projectResponse.Content.ReadFromJsonAsync<CreateProject.Response>();
        var projectId = createdProject!.Project.Id;

        // 2. Setup: Create Contract
        var createContractRequest = new CreateProjectContract.Request(
            "Test Contract Manager",
            "REG-CONT-002"
        );
        var contractResponse = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", createContractRequest);
        Assert.Equal(HttpStatusCode.Created, contractResponse.StatusCode);
        var createdContract = await contractResponse.Content.ReadFromJsonAsync<CreateProjectContract.Response>();
        var contractId = createdContract!.ProjectContract.Id;

        // 3. Setup: Create Employee
        Guid managerId = await SeedEmployeeAsync("8888", "Jane Manager", "jane.manager@contracts.com");

        // 4. POST /api/contracts/{contractId}/managers
        var addManagerRequest = new AddContractManager.Request(
            contractId,
            managerId
        );
        var addResponse = await Client.PostAsJsonAsync($"/api/contracts/{contractId}/managers", addManagerRequest);
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        // 5. GET /api/projects/{projectId}/contracts/managers
        var getManagersResponse = await Client.GetAsync($"/api/projects/{projectId}/contracts/managers");
        Assert.Equal(HttpStatusCode.OK, getManagersResponse.StatusCode);
        var managersList = await getManagersResponse.Content.ReadFromJsonAsync<GetProjectContractsManagers.Response>();
        Assert.NotNull(managersList);
        Assert.Contains(managersList!.Managers, m => m.EmployeeId == managerId && m.ContractId == contractId);

        // 6. DELETE /api/contracts/{contractId}/managers/{managerId}
        var deleteResponse = await Client.DeleteAsync($"/api/contracts/{contractId}/managers/{managerId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // 7. Verify Manager is removed
        var getManagersAfterDeleteResponse = await Client.GetAsync($"/api/projects/{projectId}/contracts/managers");
        var managersListAfterDelete = await getManagersAfterDeleteResponse.Content.ReadFromJsonAsync<GetProjectContractsManagers.Response>();
        Assert.DoesNotContain(managersListAfterDelete!.Managers, m => m.EmployeeId == managerId && m.ContractId == contractId);
    }
}
