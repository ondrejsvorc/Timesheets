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

public class ContractCatalogTests : BaseIntegrationTest
{
    public ContractCatalogTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Contract_Catalog_Returns_Contracts()
    {
        // 1. Setup: Create Project
        var createProjectRequest = new CreateProject.Request(
            "Test Project for Catalog",
            "REG-CAT-001",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(30)
        );
        var projectResponse = await Client.PostAsJsonAsync("/api/projects", createProjectRequest);
        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);
        var createdProject = await projectResponse.Content.ReadFromJsonAsync<CreateProject.Response>();
        var projectId = createdProject!.Project.Id;

        // 2. Setup: Create Contract
        var createContractRequest = new CreateProjectContract.Request(
            "Test Contract Catalog 1",
            "REG-CONT-CAT-001"
        );
        var contractResponse = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", createContractRequest);
        Assert.Equal(HttpStatusCode.Created, contractResponse.StatusCode);
        var createdContract = await contractResponse.Content.ReadFromJsonAsync<CreateProjectContract.Response>();
        var contractId = createdContract!.ProjectContract.Id;

        // 3. GET /api/contracts/catalog
        var getCatalogResponse = await Client.GetAsync("/api/contracts/catalog");
        Assert.Equal(HttpStatusCode.OK, getCatalogResponse.StatusCode);
        
        var catalog = await getCatalogResponse.Content.ReadFromJsonAsync<GetContractCatalog.Response>();
        Assert.NotNull(catalog);
        Assert.Contains(catalog!.Contracts, c => c.Id == contractId && c.ProjectId == projectId);

        // 4. GET /api/contracts/catalog?projectId={projectId}
        var getFilteredCatalogResponse = await Client.GetAsync($"/api/contracts/catalog?projectId={projectId}");
        Assert.Equal(HttpStatusCode.OK, getFilteredCatalogResponse.StatusCode);
        
        var filteredCatalog = await getFilteredCatalogResponse.Content.ReadFromJsonAsync<GetContractCatalog.Response>();
        Assert.NotNull(filteredCatalog);
        Assert.Contains(filteredCatalog!.Contracts, c => c.Id == contractId);
        Assert.All(filteredCatalog.Contracts, c => Assert.Equal(projectId, c.ProjectId));
    }
}
