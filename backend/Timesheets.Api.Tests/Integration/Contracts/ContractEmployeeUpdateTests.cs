using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Contracts;
using Timesheets.Api.Contracts.Endpoints;
using Timesheets.Api.Data;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Contracts;

public class ContractEmployeeUpdateTests : BaseIntegrationTest
{
    public ContractEmployeeUpdateTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task UpdateImpact_Split_ReturnsCreatesNewAssignment()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        ContractEmployeeUpdateRequest request = new(TestIdentifiers.Position(1), "Senior Developer", 1.0m, new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/contracts/{setup.ContractId}/employees/{setup.ContractEmployeeId}/update-impact", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ContractEmployeeUpdateImpact? impact = await response.Content.ReadFromJsonAsync<ContractEmployeeUpdateImpact>();
        Assert.NotNull(impact);
        Assert.True(impact!.CanUpdate);
        Assert.True(impact.CreatesNewAssignment);
        Assert.Equal(new DateTime(2024, 5, 31, 0, 0, 0, DateTimeKind.Utc), impact.CurrentAssignmentEndDate);
        Assert.Equal(new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc), impact.NewAssignmentStartDate);
    }

    [Fact]
    public async Task UpdateContractEmployee_Split_EndsOldAndCreatesNew()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        UpdateContractEmployee.Request request = new(TestIdentifiers.Position(1), "Senior Developer", 1.0m, new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/contracts/{setup.ContractId}/employees/{setup.ContractEmployeeId}", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        UpdateContractEmployee.Response? updated = await response.Content.ReadFromJsonAsync<UpdateContractEmployee.Response>();
        Assert.NotNull(updated);
        Assert.Equal("Senior Developer", updated!.Position);
        Assert.Equal(new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc), updated.StartDate);
        Assert.NotEqual(setup.ContractEmployeeId, updated.Id);

        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Data.Models.ContractEmployee? oldAssignment = await dbContext.ContractEmployees.AsNoTracking().SingleOrDefaultAsync(assignment => assignment.Id == setup.ContractEmployeeId);
        Assert.NotNull(oldAssignment);
        Assert.Equal(new DateTime(2024, 5, 31, 0, 0, 0, DateTimeKind.Utc), oldAssignment!.EndDate);
    }

    [Fact]
    public async Task UpdateImpact_ShortenEnd_WithDraftDaysOutside_ReturnsDraftDaysToRemove()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        ContractEmployeeUpdateRequest request = new(TestIdentifiers.Position(1), "Developer", 1.0m, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 11, 30, 0, 0, 0, DateTimeKind.Utc));
        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/contracts/{setup.ContractId}/employees/{setup.ContractEmployeeId}/update-impact", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ContractEmployeeUpdateImpact? impact = await response.Content.ReadFromJsonAsync<ContractEmployeeUpdateImpact>();
        Assert.NotNull(impact);
        Assert.True(impact!.CanUpdate);
        Assert.False(impact.CreatesNewAssignment);
        Assert.True(impact.DraftDaysToRemove > 0);
    }

    [Fact]
    public async Task UpdateContractEmployee_ShortenEnd_RemovesDraftDaysOutsideRange()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        UpdateContractEmployee.Request request = new(TestIdentifiers.Position(1), "Developer", 1.0m, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 11, 30, 0, 0, 0, DateTimeKind.Utc));
        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/contracts/{setup.ContractId}/employees/{setup.ContractEmployeeId}", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        int decemberDayCount = await dbContext.ProjectDays.AsNoTracking().Where(day => day.ProjectTimesheet.ContractEmployeeId == setup.ContractEmployeeId).Where(day => day.Date > new DateTime(2024, 11, 30, 0, 0, 0, DateTimeKind.Utc)).CountAsync();
        Assert.Equal(0, decemberDayCount);
    }

    [Fact]
    public async Task UpdateImpact_ShortenEnd_WithSubmittedOutside_ReturnsBlocked()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        Guid decemberTimesheetId = await GetProjectTimesheetIdAsync(setup.ContractEmployeeId, 2024, 12);
        await SetProjectTimesheetStatusAsync(decemberTimesheetId, TestTimesheetStatusIds.Submitted);

        ContractEmployeeUpdateRequest request = new(TestIdentifiers.Position(1), "Developer", 1.0m, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 11, 30, 0, 0, 0, DateTimeKind.Utc));
        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/contracts/{setup.ContractId}/employees/{setup.ContractEmployeeId}/update-impact", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ContractEmployeeUpdateImpact? impact = await response.Content.ReadFromJsonAsync<ContractEmployeeUpdateImpact>();
        Assert.NotNull(impact);
        Assert.False(impact!.CanUpdate);
        Assert.NotNull(impact.BlockReason);
    }

    [Fact]
    public async Task UpdateImpact_Unchanged_ReturnsBlocked()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        ContractEmployeeUpdateRequest request = new(TestIdentifiers.Position(1), "Developer", 1.0m, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/contracts/{setup.ContractId}/employees/{setup.ContractEmployeeId}/update-impact", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ContractEmployeeUpdateImpact? impact = await response.Content.ReadFromJsonAsync<ContractEmployeeUpdateImpact>();
        Assert.NotNull(impact);
        Assert.False(impact!.CanUpdate);
    }

    [Fact]
    public async Task UpdateImpact_MetadataOnly_ReturnsCanUpdate()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        ContractEmployeeUpdateRequest request = new(TestIdentifiers.Position(1), "Senior Developer", 1.0m, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/contracts/{setup.ContractId}/employees/{setup.ContractEmployeeId}/update-impact", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ContractEmployeeUpdateImpact? impact = await response.Content.ReadFromJsonAsync<ContractEmployeeUpdateImpact>();
        Assert.NotNull(impact);
        Assert.True(impact!.CanUpdate);
        Assert.False(impact.CreatesNewAssignment);
    }

    [Fact]
    public async Task UpdateContractEmployee_MetadataOnly_UpdatesSameAssignment()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        UpdateContractEmployee.Request request = new(TestIdentifiers.Position(1), "Senior Developer", 1.0m, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/contracts/{setup.ContractId}/employees/{setup.ContractEmployeeId}", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        UpdateContractEmployee.Response? updated = await response.Content.ReadFromJsonAsync<UpdateContractEmployee.Response>();
        Assert.NotNull(updated);
        Assert.Equal(setup.ContractEmployeeId, updated!.Id);
        Assert.Equal("Senior Developer", updated.Position);
    }

    [Fact]
    public async Task UpdateImpact_ExtendEnd_ReturnsNewMonths()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 6, 30, 0, 0, 0, DateTimeKind.Utc));
        ContractEmployeeUpdateRequest request = new(TestIdentifiers.Position(1), "Developer", 1.0m, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/contracts/{setup.ContractId}/employees/{setup.ContractEmployeeId}/update-impact", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ContractEmployeeUpdateImpact? impact = await response.Content.ReadFromJsonAsync<ContractEmployeeUpdateImpact>();
        Assert.NotNull(impact);
        Assert.True(impact!.CanUpdate);
        Assert.False(impact.CreatesNewAssignment);
        Assert.True(impact.NewTimesheetMonthCount > 0);
    }

    private async Task<Guid> GetProjectTimesheetIdAsync(Guid contractEmployeeId, int year, int month)
    {
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.ProjectTimesheets.AsNoTracking().Where(timesheet => timesheet.ContractEmployeeId == contractEmployeeId).Where(timesheet => timesheet.Year == year && timesheet.Month == month).Select(timesheet => timesheet.Id).SingleAsync();
    }

    private async Task SetProjectTimesheetStatusAsync(Guid projectTimesheetId, Guid statusId)
    {
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        int affected = await dbContext.ProjectTimesheets.Where(timesheet => timesheet.Id == projectTimesheetId).ExecuteUpdateAsync(setters => setters.SetProperty(timesheet => timesheet.TimesheetStatusId, statusId));
        Assert.Equal(1, affected);
    }
}
