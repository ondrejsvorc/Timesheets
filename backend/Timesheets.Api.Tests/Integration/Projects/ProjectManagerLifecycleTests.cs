using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Timesheets.Api.Features.Projects.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Projects;

public class ProjectManagerLifecycleTests : BaseIntegrationTest
{
    public ProjectManagerLifecycleTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Project_Manager_Lifecycle_HappyPath_CompletesSuccessfully()
    {
        CreateProject.Request createProjectRequest = new("Test Project for Project Managers", TestIdentifiers.Project(1010), DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30));
        HttpResponseMessage projectResponse = await Client.PostAsJsonAsync("/api/projects", createProjectRequest);
        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);
        CreateProject.Response projectBody = (await projectResponse.Content.ReadFromJsonAsync<CreateProject.Response>())!;
        Guid contractEmployeeId = projectBody.Project.Id;

        string managerPersonalNumber = "pm-" + TestIdentifiers.Suffix(17);
        Guid managerId = await SeedEmployeeAsync(managerPersonalNumber, "Jane", "Manager");

        HttpResponseMessage addResponse = await Client.PostAsJsonAsync($"/api/projects/{contractEmployeeId}/managers", new AddProjectManager.Request(contractEmployeeId, managerId));
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        GetProjectManagers.Response? managersList = await (await Client.GetAsync($"/api/projects/{contractEmployeeId}/managers")).Content.ReadFromJsonAsync<GetProjectManagers.Response>();
        Assert.NotNull(managersList);
        Assert.Contains(managersList!.Managers, manager => manager.EmployeeId == managerId && manager.ProjectId == contractEmployeeId);

        using WebApplicationFactory<Program> managerFactory = CreateAuthenticatedFactory();
        using HttpClient managerClient = managerFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        managerClient.DefaultRequestHeaders.Add(TestAuthHandler.PersonalNumberHeader, managerPersonalNumber);

        UpdateProject.Request updateRequest = new("Updated by Project Manager", projectBody.Project.RegistrationNumber, projectBody.Project.StartDate, projectBody.Project.EndDate);
        HttpResponseMessage updateResponse = await managerClient.PutAsJsonAsync($"/api/projects/{contractEmployeeId}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        HttpResponseMessage forbiddenDeleteResponse = await managerClient.DeleteAsync($"/api/projects/{contractEmployeeId}");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenDeleteResponse.StatusCode);

        HttpResponseMessage deleteResponse = await Client.DeleteAsync($"/api/projects/{contractEmployeeId}/managers/{managerId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        GetProjectManagers.Response? managersAfterDelete = await (await Client.GetAsync($"/api/projects/{contractEmployeeId}/managers")).Content.ReadFromJsonAsync<GetProjectManagers.Response>();
        Assert.DoesNotContain(managersAfterDelete!.Managers, manager => manager.EmployeeId == managerId && manager.ProjectId == contractEmployeeId);
    }

    private WebApplicationFactory<Program> CreateAuthenticatedFactory() => Factory.WithWebHostBuilder(builder =>
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Enabled"] = "true"
            })));
}
