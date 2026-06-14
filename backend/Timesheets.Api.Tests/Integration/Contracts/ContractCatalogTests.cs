using System.Net;
using System.Net.Http.Json;
using Timesheets.Api.Contracts.Endpoints;
using Timesheets.Api.Projects.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Contracts;

public class ContractCatalogTests : BaseIntegrationTest
{
    public ContractCatalogTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Contract_Catalog_Returns_Contracts()
    {
        CreateProject.Request createProjectRequest = new("Test Project for Catalog", "REG-CAT-001", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30));
        HttpResponseMessage projectResponse = await Client.PostAsJsonAsync("/api/projects", createProjectRequest);
        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);
        Guid projectId = (await projectResponse.Content.ReadFromJsonAsync<CreateProject.Response>())!.Project.Id;

        CreateProjectContract.Request createContractRequest = new("Test Contract Catalog 1", "REG-CONT-CAT-001");
        HttpResponseMessage contractResponse = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", createContractRequest);
        Assert.Equal(HttpStatusCode.Created, contractResponse.StatusCode);
        Guid contractId = (await contractResponse.Content.ReadFromJsonAsync<CreateProjectContract.Response>())!.ProjectContract.Id;

        GetContractCatalog.Response? catalog = await (await Client.GetAsync("/api/contracts/catalog")).Content.ReadFromJsonAsync<GetContractCatalog.Response>();
        Assert.NotNull(catalog);
        Assert.Contains(catalog!.Contracts, contract => contract.Id == contractId && contract.ProjectId == projectId);

        GetContractCatalog.Response? filteredCatalog = await (await Client.GetAsync($"/api/contracts/catalog?projectId={projectId}")).Content.ReadFromJsonAsync<GetContractCatalog.Response>();
        Assert.NotNull(filteredCatalog);
        Assert.Contains(filteredCatalog!.Contracts, contract => contract.Id == contractId);
        Assert.All(filteredCatalog.Contracts, contract => Assert.Equal(projectId, contract.ProjectId));
    }
}
