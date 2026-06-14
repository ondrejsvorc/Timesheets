using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Contracts;
using Timesheets.Api.Data;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Contracts;

public class ContractDeleteProtectionTests : BaseIntegrationTest
{
    public ContractDeleteProtectionTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetContractDeleteImpact_WithDraftTimesheets_AllowsDelete()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 2, 28, 0, 0, 0, DateTimeKind.Utc));
        HttpResponseMessage response = await Client.GetAsync($"/api/contracts/{setup.ContractId}/delete-impact");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ContractDeleteImpact? impact = await response.Content.ReadFromJsonAsync<ContractDeleteImpact>();
        Assert.NotNull(impact);
        Assert.True(impact!.CanDelete);
        Assert.False(impact.HasProtectedTimesheets);
        Assert.True(impact.DraftProjectTimesheetCount > 0);
        Assert.True(impact.PositionCount > 0);
    }

    [Fact]
    public async Task GetContractDeleteImpact_WithSubmittedTimesheets_ReportsProtectedCounts()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 7, 31, 0, 0, 0, DateTimeKind.Utc));
        Guid projectTimesheetId = await GetSingleProjectTimesheetIdAsync(setup.ContractEmployeeId);
        await SetProjectTimesheetStatusAsync(projectTimesheetId, TestTimesheetStatusIds.Submitted);
        HttpResponseMessage response = await Client.GetAsync($"/api/contracts/{setup.ContractId}/delete-impact");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ContractDeleteImpact? impact = await response.Content.ReadFromJsonAsync<ContractDeleteImpact>();
        Assert.NotNull(impact);
        Assert.False(impact!.CanDelete);
        Assert.True(impact.HasProtectedTimesheets);
        Assert.Equal(1, impact.SubmittedProjectTimesheetCount);
        Assert.True(impact.CanForceDelete);
    }

    [Fact]
    public async Task GetContractDeleteImpact_DoesNotIncludeContractCount()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 8, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 8, 31, 0, 0, 0, DateTimeKind.Utc));
        string json = await Client.GetStringAsync($"/api/contracts/{setup.ContractId}/delete-impact");
        Assert.DoesNotContain("contractCount", json, StringComparison.OrdinalIgnoreCase);
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
