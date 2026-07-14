using System.Net;
using System.Net.Http.Json;
using Timesheets.Api.Features.Contracts.Endpoints;
using Timesheets.Api.Features.Projects.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Contracts;

public class ContractManagerLifecycleTests : BaseIntegrationTest
{
    public ContractManagerLifecycleTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Contract_Manager_Lifecycle_HappyPath_CompletesSuccessfully()
    {
        CreateProject.Request createProjectRequest = new("Test Project for Contract Managers", TestIdentifiers.Project(1050), DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30));
        HttpResponseMessage projectResponse = await Client.PostAsJsonAsync("/api/projects", createProjectRequest);
        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);
        Guid contractEmployeeId = (await projectResponse.Content.ReadFromJsonAsync<CreateProject.Response>())!.Project.Id;

        CreateProjectContract.Request createContractRequest = new("Test Contract Manager", TestIdentifiers.Contract(1050));
        HttpResponseMessage contractResponse = await Client.PostAsJsonAsync($"/api/projects/{contractEmployeeId}/contracts", createContractRequest);
        Assert.Equal(HttpStatusCode.Created, contractResponse.StatusCode);
        Guid contractId = (await contractResponse.Content.ReadFromJsonAsync<CreateProjectContract.Response>())!.ProjectContract.Id;

        Guid managerId = await SeedEmployeeAsync("8888", "Jane", "Manager");
        HttpResponseMessage addResponse = await Client.PostAsJsonAsync($"/api/contracts/{contractId}/managers", new AddContractManager.Request(contractId, managerId));
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        GetProjectContractsManagers.Response? managersList = await (await Client.GetAsync($"/api/projects/{contractEmployeeId}/contracts/managers")).Content.ReadFromJsonAsync<GetProjectContractsManagers.Response>();
        Assert.NotNull(managersList);
        Assert.Contains(managersList!.Managers, manager => manager.EmployeeId == managerId && manager.ContractId == contractId);

        HttpResponseMessage deleteResponse = await Client.DeleteAsync($"/api/contracts/{contractId}/managers/{managerId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        GetProjectContractsManagers.Response? managersAfterDelete = await (await Client.GetAsync($"/api/projects/{contractEmployeeId}/contracts/managers")).Content.ReadFromJsonAsync<GetProjectContractsManagers.Response>();
        Assert.DoesNotContain(managersAfterDelete!.Managers, manager => manager.EmployeeId == managerId && manager.ContractId == contractId);
    }
}
