using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;
using Timesheets.Api.Projects.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Projects;

public class CreateProjectContractTests : BaseIntegrationTest
{
    public CreateProjectContractTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<Guid> CreateProjectAsync()
    {
        string suffix = Guid.NewGuid().ToString("N")[..8];
        CreateProject.Request request = new($"Test Project For Contract {suffix}", $"REG-{suffix}", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30));
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/projects", request);
        CreateProject.Response? content = await response.Content.ReadFromJsonAsync<CreateProject.Response>();
        return content!.Project.Id;
    }

    [Fact]
    public async Task CreateProjectContract_WithValidData_ReturnsCreated()
    {
        Guid projectId = await CreateProjectAsync();
        CreateProjectContract.Request request = new("Test Contract", "12345 12 1234 12");
        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        CreateProjectContract.Response? content = await response.Content.ReadFromJsonAsync<CreateProjectContract.Response>();
        Assert.Equal(request.RegistrationNumber, content!.ProjectContract.RegistrationNumber);
    }

    [Fact]
    public async Task CreateProjectContract_WithEmptyName_ReturnsBadRequest()
    {
        Guid projectId = await CreateProjectAsync();
        CreateProjectContract.Request request = new("", "CONT-002");
        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProjectContract_WithLongName_ReturnsBadRequest()
    {
        Guid projectId = await CreateProjectAsync();
        CreateProjectContract.Request request = new(new string('A', 201), "CONT-003");
        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProjectContract_WithLongRegistrationNumber_ReturnsBadRequest()
    {
        Guid projectId = await CreateProjectAsync();
        CreateProjectContract.Request request = new("Valid Name", new string('B', 101));
        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProjectContract_WithDuplicateRegistrationNumberInSameProject_ReturnsBadRequest()
    {
        Guid projectId = await CreateProjectAsync();
        CreateProjectContract.Request first = new("First Contract", "CONT-DUP-001");
        HttpResponseMessage firstResponse = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", first);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        CreateProjectContract.Request duplicate = new("Second Contract", "  cont-dup-001  ");
        HttpResponseMessage duplicateResponse = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", duplicate);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);
        Assert.Contains("existuje", await duplicateResponse.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateProjectContract_WithDuplicateNameInSameProject_ReturnsBadRequest()
    {
        Guid projectId = await CreateProjectAsync();
        CreateProjectContract.Request first = new("Duplicate Contract", "CONT-DUP-002");
        HttpResponseMessage firstResponse = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", first);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        CreateProjectContract.Request duplicate = new("  duplicate contract  ", "CONT-DUP-003");
        HttpResponseMessage duplicateResponse = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", duplicate);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);
        Assert.Contains("existuje", await duplicateResponse.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateProjectContract_WithSameRegistrationNumberInDifferentProject_ReturnsCreated()
    {
        Guid firstProjectId = await CreateProjectAsync();
        CreateProjectContract.Request first = new("Shared Id Contract", "CONT-SHARED-001");
        HttpResponseMessage firstResponse = await Client.PostAsJsonAsync($"/api/projects/{firstProjectId}/contracts", first);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        Guid secondProjectId = await CreateProjectAsync();
        CreateProjectContract.Request second = new("Other Contract", "CONT-SHARED-001");
        HttpResponseMessage secondResponse = await Client.PostAsJsonAsync($"/api/projects/{secondProjectId}/contracts", second);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
    }

    [Fact]
    public async Task DatabaseRejectsNormalizedDuplicateContract()
    {
        Guid projectId = await CreateProjectAsync();
        HttpResponseMessage firstResponse = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", new CreateProjectContract.Request("Database Contract", "DB-001"));
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Contracts.Add(new Contract { Id = Guid.NewGuid(), ProjectId = projectId, Name = "  database contract  ", RegistrationNumber = "DB-002" });

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }
}
