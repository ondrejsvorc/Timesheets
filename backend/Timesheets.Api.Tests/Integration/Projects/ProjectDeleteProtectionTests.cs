using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Data;
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
