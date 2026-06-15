using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Contracts.Endpoints;
using Timesheets.Api.Data;
using Timesheets.Api.Projects.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Contracts;

public class ContractEmployeeLifecycleTests : BaseIntegrationTest
{
    public ContractEmployeeLifecycleTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Contract_Employee_Lifecycle_HappyPath_CompletesSuccessfully()
    {
        CreateProject.Request createProjectRequest = new("Test Project for Contract Employees", "REG-CON-EMP-001", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30));
        HttpResponseMessage projectResponse = await Client.PostAsJsonAsync("/api/projects", createProjectRequest);
        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);
        Guid projectId = (await projectResponse.Content.ReadFromJsonAsync<CreateProject.Response>())!.Project.Id;

        CreateProjectContract.Request createContractRequest = new("Test Contract Employee", "REG-CONT-001");
        HttpResponseMessage contractResponse = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", createContractRequest);
        Assert.Equal(HttpStatusCode.Created, contractResponse.StatusCode);
        Guid contractId = (await contractResponse.Content.ReadFromJsonAsync<CreateProjectContract.Response>())!.ProjectContract.Id;

        Guid employeeId = await SeedEmployeeAsync("9999", "John Doe Contract", "john.doe@contracts.com");
        AddContractEmployee.Request addEmployeeRequest = new(employeeId, "POS-01", "Developer", 1.0m, DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(15));
        HttpResponseMessage addResponse = await Client.PostAsJsonAsync($"/api/contracts/{contractId}/employees", addEmployeeRequest);
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        GetContractEmployees.Response? employeesList = await (await Client.GetAsync($"/api/contracts/{contractId}/employees")).Content.ReadFromJsonAsync<GetContractEmployees.Response>();
        Assert.NotNull(employeesList);
        Assert.Contains(employeesList!.Employees, employee => employee.Id == employeeId && employee.Positions.Any(position => position.PositionCode == "POS-01"));
        Guid contractEmployeeId = employeesList.Employees.First(employee => employee.Id == employeeId).Positions.First(position => position.PositionCode == "POS-01").Id;

        HttpResponseMessage deleteResponse = await Client.DeleteAsync($"/api/contracts/{contractId}/employees/{contractEmployeeId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        GetContractEmployees.Response? employeesAfterDelete = await (await Client.GetAsync($"/api/contracts/{contractId}/employees")).Content.ReadFromJsonAsync<GetContractEmployees.Response>();
        Assert.DoesNotContain(employeesAfterDelete!.Employees, employee => employee.Id == employeeId && employee.Positions.Any(position => position.Id == contractEmployeeId));
    }

    [Fact]
    public async Task RemoveContractEmployee_WithSubmittedTimesheet_ReturnsConflict()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 31, 0, 0, 0, DateTimeKind.Utc));

        using (IServiceScope scope = CreateScope())
        {
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.ProjectTimesheets.Where(timesheet => timesheet.ContractEmployeeId == setup.ContractEmployeeId).ExecuteUpdateAsync(setters => setters.SetProperty(timesheet => timesheet.TimesheetStatusId, TestTimesheetStatusIds.Submitted));
        }

        HttpResponseMessage response = await Client.DeleteAsync($"/api/contracts/{setup.ContractId}/employees/{setup.ContractEmployeeId}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using IServiceScope assertionScope = CreateScope();
        AppDbContext assertionContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await assertionContext.ContractEmployees.AnyAsync(position => position.Id == setup.ContractEmployeeId));
    }

    [Fact]
    public async Task GetContractEmployees_PositionEndingToday_IsActive()
    {
        DateTime czechToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague")).Date;
        DateTime today = new(czechToday.Year, czechToday.Month, czechToday.Day, 0, 0, 0, DateTimeKind.Utc);
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, today.AddDays(-1), today);

        GetContractEmployees.Response? response = await (await Client.GetAsync($"/api/contracts/{setup.ContractId}/employees")).Content.ReadFromJsonAsync<GetContractEmployees.Response>();
        GetContractEmployees.PositionItem position = Assert.Single(response!.Employees.Single(employee => employee.Id == setup.EmployeeId).Positions);

        Assert.True(position.IsActive);
    }
}
