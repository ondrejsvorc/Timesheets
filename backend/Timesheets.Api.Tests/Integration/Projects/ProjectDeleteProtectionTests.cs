using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Data;
using Timesheets.Api.Projects;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Projects;

public class ProjectDeleteProtectionTests : BaseIntegrationTest
{
    public ProjectDeleteProtectionTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task DeleteProject_WithOnlyDraftTimesheets_ReturnsNoContent()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 3, 31, 0, 0, 0, DateTimeKind.Utc));
        HttpResponseMessage response = await Client.DeleteAsync($"/api/projects/{setup.ProjectId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GetProjectDeleteImpact_WithDraftTimesheets_AllowsDelete()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 2, 28, 0, 0, 0, DateTimeKind.Utc));
        HttpResponseMessage response = await Client.GetAsync($"/api/projects/{setup.ProjectId}/delete-impact");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ProjectDeleteImpact? impact = await response.Content.ReadFromJsonAsync<ProjectDeleteImpact>();
        Assert.NotNull(impact);
        Assert.True(impact!.CanDelete);
        Assert.False(impact.HasProtectedTimesheets);
        Assert.True(impact.DraftProjectTimesheetCount > 0);
        Assert.Equal(1, impact.ContractCount);
    }

    [Fact]
    public async Task DeleteProject_WithSubmittedTimesheets_ReturnsConflict()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 2, 28, 0, 0, 0, DateTimeKind.Utc));
        Guid projectTimesheetId = await GetSingleProjectTimesheetIdAsync(setup.ContractEmployeeId);
        await SetProjectTimesheetStatusAsync(projectTimesheetId, TestTimesheetStatusIds.Submitted);
        HttpResponseMessage response = await Client.DeleteAsync($"/api/projects/{setup.ProjectId}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_WithSubmittedTimesheetsAndForceQuery_ReturnsConflict()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 4, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 5, 31, 0, 0, 0, DateTimeKind.Utc));
        Guid projectTimesheetId = await GetSingleProjectTimesheetIdAsync(setup.ContractEmployeeId);
        await SetProjectTimesheetStatusAsync(projectTimesheetId, TestTimesheetStatusIds.Submitted);
        HttpResponseMessage response = await Client.DeleteAsync($"/api/projects/{setup.ProjectId}?force=true");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetProjectDeleteImpact_WithSubmittedTimesheets_ReportsProtectedCounts()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 7, 31, 0, 0, 0, DateTimeKind.Utc));
        Guid projectTimesheetId = await GetSingleProjectTimesheetIdAsync(setup.ContractEmployeeId);
        await SetProjectTimesheetStatusAsync(projectTimesheetId, TestTimesheetStatusIds.Submitted);
        HttpResponseMessage response = await Client.GetAsync($"/api/projects/{setup.ProjectId}/delete-impact");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ProjectDeleteImpact? impact = await response.Content.ReadFromJsonAsync<ProjectDeleteImpact>();
        Assert.NotNull(impact);
        Assert.False(impact!.CanDelete);
        Assert.True(impact.HasProtectedTimesheets);
        Assert.Equal(1, impact.SubmittedProjectTimesheetCount);
    }

    [Fact]
    public async Task GetProjectContractDeleteImpactRoute_IsRemoved()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 9, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 9, 30, 0, 0, 0, DateTimeKind.Utc));
        HttpResponseMessage response = await Client.GetAsync($"/api/projects/{setup.ProjectId}/contracts/{setup.ContractId}/delete-impact");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> GetSingleProjectTimesheetIdAsync(Guid contractEmployeeId)
    {
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.ProjectTimesheets.AsNoTracking().Where(timesheet => timesheet.ContractEmployeeId == contractEmployeeId).Select(timesheet => timesheet.Id).FirstAsync();
    }

    private async Task SetProjectTimesheetStatusAsync(Guid projectTimesheetId, Guid statusId)
    {
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        int affected = await dbContext.ProjectTimesheets.Where(timesheet => timesheet.Id == projectTimesheetId).ExecuteUpdateAsync(setters => setters.SetProperty(timesheet => timesheet.TimesheetStatusId, statusId));
        Assert.Equal(1, affected);
    }
}
