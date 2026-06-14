using System.Net;
using System.Net.Http.Json;
using Timesheets.Api.Contracts.Endpoints;
using Timesheets.Api.Projects.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Contracts;

public class ContractManagerLifecycleTests : BaseIntegrationTest
{
    public ContractManagerLifecycleTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Contract_Manager_Lifecycle_HappyPath_CompletesSuccessfully()
    {
        CreateProject.Request createProjectRequest = new("Test Project for Contract Managers", "REG-CON-MAN-001", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30));
        HttpResponseMessage projectResponse = await Client.PostAsJsonAsync("/api/projects", createProjectRequest);
        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);
        Guid projectId = (await projectResponse.Content.ReadFromJsonAsync<CreateProject.Response>())!.Project.Id;

        CreateProjectContract.Request createContractRequest = new("Test Contract Manager", "REG-CONT-002");
        HttpResponseMessage contractResponse = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", createContractRequest);
        Assert.Equal(HttpStatusCode.Created, contractResponse.StatusCode);
        Guid contractId = (await contractResponse.Content.ReadFromJsonAsync<CreateProjectContract.Response>())!.ProjectContract.Id;

        Guid managerId = await SeedEmployeeAsync("8888", "Jane Manager", "jane.manager@contracts.com");
        HttpResponseMessage addResponse = await Client.PostAsJsonAsync($"/api/contracts/{contractId}/managers", new AddContractManager.Request(contractId, managerId));
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        GetProjectContractsManagers.Response? managersList = await (await Client.GetAsync($"/api/projects/{projectId}/contracts/managers")).Content.ReadFromJsonAsync<GetProjectContractsManagers.Response>();
        Assert.NotNull(managersList);
        Assert.Contains(managersList!.Managers, manager => manager.EmployeeId == managerId && manager.ContractId == contractId);

        HttpResponseMessage deleteResponse = await Client.DeleteAsync($"/api/contracts/{contractId}/managers/{managerId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        GetProjectContractsManagers.Response? managersAfterDelete = await (await Client.GetAsync($"/api/projects/{projectId}/contracts/managers")).Content.ReadFromJsonAsync<GetProjectContractsManagers.Response>();
        Assert.DoesNotContain(managersAfterDelete!.Managers, manager => manager.EmployeeId == managerId && manager.ContractId == contractId);
    }
}
