using System.Net;
using System.Net.Http.Json;
using Timesheets.Api.Features.Contracts.Endpoints;
using Timesheets.Api.Features.Projects;
using Timesheets.Api.Features.Projects.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Projects;

public class ProjectArchiveTests : BaseIntegrationTest
{
    public ProjectArchiveTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task ArchiveProject_SetsArchivedAt()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        HttpResponseMessage response = await Client.PostAsync($"/api/projects/{setup.ProjectId}/archive", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ArchiveProject.Response? payload = await response.Content.ReadFromJsonAsync<ArchiveProject.Response>();
        Assert.NotNull(payload);
        Assert.NotNull(payload!.Project.ArchivedAt);

        GetProjects.Response? projects = await (await Client.GetAsync("/api/projects")).Content.ReadFromJsonAsync<GetProjects.Response>();
        Assert.NotNull(projects);
        ProjectItem project = projects!.Projects.Single(item => item.Id == setup.ProjectId);
        Assert.NotNull(project.ArchivedAt);
    }

    [Fact]
    public async Task ArchiveProject_WhenAlreadyArchived_ReturnsBadRequest()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        HttpResponseMessage firstResponse = await Client.PostAsync($"/api/projects/{setup.ProjectId}/archive", null);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        HttpResponseMessage secondResponse = await Client.PostAsync($"/api/projects/{setup.ProjectId}/archive", null);
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
    }

    [Fact]
    public async Task UnarchiveProject_ClearsArchivedAt()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        HttpResponseMessage archiveResponse = await Client.PostAsync($"/api/projects/{setup.ProjectId}/archive", null);
        Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);
        HttpResponseMessage unarchiveResponse = await Client.PostAsync($"/api/projects/{setup.ProjectId}/unarchive", null);
        Assert.Equal(HttpStatusCode.OK, unarchiveResponse.StatusCode);

        UnarchiveProject.Response? payload = await unarchiveResponse.Content.ReadFromJsonAsync<UnarchiveProject.Response>();
        Assert.NotNull(payload);
        Assert.Null(payload!.Project.ArchivedAt);
    }

    [Fact]
    public async Task UnarchiveProject_WhenNotArchived_ReturnsBadRequest()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        HttpResponseMessage response = await Client.PostAsync($"/api/projects/{setup.ProjectId}/unarchive", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ArchivedProject_BlocksWrites()
    {
        DateTime start = new(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime end = new(2024, 5, 31, 0, 0, 0, DateTimeKind.Utc);
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, start, end);

        HttpResponseMessage archiveResponse = await Client.PostAsync($"/api/projects/{setup.ProjectId}/archive", null);
        Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);

        HttpResponseMessage updateProject = await Client.PutAsJsonAsync(
            $"/api/projects/{setup.ProjectId}",
            new UpdateProject.Request("Archived Project Update", TestIdentifiers.Project(12001), start, end.AddYears(1)));
        Assert.Equal(HttpStatusCode.BadRequest, updateProject.StatusCode);

        HttpResponseMessage createContract = await Client.PostAsJsonAsync(
            $"/api/projects/{setup.ProjectId}/contracts",
            new CreateProjectContract.Request("Archived Contract", TestIdentifiers.Contract(12001)));
        Assert.Equal(HttpStatusCode.BadRequest, createContract.StatusCode);

        HttpResponseMessage addPosition = await Client.PostAsJsonAsync(
            $"/api/contracts/{setup.ContractId}/employees",
            new AddContractEmployee.Request(Guid.CreateVersion7(), TestIdentifiers.Position(12001), "Blocked", 0.1m, start, end));
        Assert.Equal(HttpStatusCode.BadRequest, addPosition.StatusCode);

        HttpResponseMessage deleteContract = await Client.DeleteAsync($"/api/projects/{setup.ProjectId}/contracts/{setup.ContractId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteContract.StatusCode);
    }
}
