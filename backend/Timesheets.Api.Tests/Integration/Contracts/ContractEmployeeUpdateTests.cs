using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Data;
using Timesheets.Api.Features.Contracts;
using Timesheets.Api.Features.Contracts.Endpoints;
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
        int decemberDayCount = await dbContext.ContractPartDays.AsNoTracking().Where(day => day.ContractPart.ContractEmployeeId == setup.ContractEmployeeId).Where(day => day.Date > new DateTime(2024, 11, 30, 0, 0, 0, DateTimeKind.Utc)).CountAsync();
        Assert.Equal(0, decemberDayCount);
    }

    [Fact]
    public async Task UpdateImpact_ShortenEnd_WithSubmittedOutside_ReturnsBlocked()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        Guid decemberTimesheetId = await GetContractPartIdAsync(setup.ContractEmployeeId, 2024, 12);
        await SetContractPartStatusAsync(decemberTimesheetId, TestTimesheetStatusIds.Submitted);

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
    public async Task UpdateContractEmployee_WorkloadChange_SyncsExistingContractParts()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(
            Factory.Services,
            Client,
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            workload: 0.5m);

        using (IServiceScope scope = CreateScope())
        {
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            decimal januaryWorkload = await dbContext.ContractParts.AsNoTracking()
                .Where(timesheet => timesheet.ContractEmployeeId == setup.ContractEmployeeId)
                .Where(timesheet => timesheet.Timesheet.Year == 2024 && timesheet.Timesheet.Month == 1)
                .Select(timesheet => timesheet.Workload)
                .SingleAsync();
            Assert.Equal(0.5m, januaryWorkload);
        }

        UpdateContractEmployee.Request request = new(
            TestIdentifiers.Position(1),
            "Developer",
            0.25m,
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/contracts/{setup.ContractId}/employees/{setup.ContractEmployeeId}", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (IServiceScope scope = CreateScope())
        {
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            decimal januaryWorkload = await dbContext.ContractParts.AsNoTracking()
                .Where(timesheet => timesheet.ContractEmployeeId == setup.ContractEmployeeId)
                .Where(timesheet => timesheet.Timesheet.Year == 2024 && timesheet.Timesheet.Month == 1)
                .Select(timesheet => timesheet.Workload)
                .SingleAsync();
            Assert.Equal(0.25m, januaryWorkload);
        }
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
    public async Task UpdateImpact_MetadataOnly_WithNullStoredEnd_ReturnsNoDateConsequences()
    {
        DateTime positionStart = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime positionEnd = new(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, positionStart, positionEnd);
        DateTime projectEnd = positionEnd.AddYears(1);
        using (IServiceScope scope = CreateScope())
        {
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Data.Models.ContractEmployee? assignment = await dbContext.ContractEmployees.SingleAsync(assignment => assignment.Id == setup.ContractEmployeeId);
            assignment.EndDate = null;
            await dbContext.SaveChangesAsync();
        }

        // UI shows coalesced end (null stored → project end), not the original position end.
        ContractEmployeeUpdateRequest request = new("NEW-CODE", "Developer", 1.0m, positionStart, projectEnd);
        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/contracts/{setup.ContractId}/employees/{setup.ContractEmployeeId}/update-impact", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ContractEmployeeUpdateImpact? impact = await response.Content.ReadFromJsonAsync<ContractEmployeeUpdateImpact>();
        Assert.NotNull(impact);
        Assert.True(impact!.CanUpdate);
        Assert.False(impact.CreatesNewAssignment);
        Assert.Null(impact.CurrentAssignmentEndDate);
        Assert.Equal(0, impact.NewTimesheetMonthCount);
        Assert.Equal(0, impact.DraftTimesheetsOnOldAssignment);
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

    private async Task<Guid> GetContractPartIdAsync(Guid contractEmployeeId, int year, int month)
    {
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.ContractParts.AsNoTracking().Where(part => part.ContractEmployeeId == contractEmployeeId && part.Timesheet.Year == year && part.Timesheet.Month == month).Select(part => part.Id).SingleAsync();
    }

    private async Task SetContractPartStatusAsync(Guid contractPartId, Guid statusId)
    {
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        int affected = await dbContext.ContractParts.Where(timesheet => timesheet.Id == contractPartId).ExecuteUpdateAsync(setters => setters.SetProperty(timesheet => timesheet.TimesheetStatusId, statusId));
        Assert.Equal(1, affected);
    }
}
