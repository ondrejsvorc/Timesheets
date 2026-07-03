using System.Net;
using System.Net.Http.Json;
using Timesheets.Api.Contracts.Endpoints;

namespace Timesheets.Api.Tests.Integration.Contracts;

public class ContractTimesheetQueryTests : BaseIntegrationTest
{
    public ContractTimesheetQueryTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetContractTimesheets_ReturnsAllOverlappingPositionsForMonth()
    {
        DateTime projectStart = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, projectStart, workload: 0.25m);
        await ImportAttendanceAsync(setup, 2026, 3, 100m);

        AddContractEmployee.Request secondPosition = new(
            setup.EmployeeId,
            TestIdentifiers.Position(2),
            "Koordinátor",
            0.4m,
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            null);
        HttpResponseMessage addPositionResponse = await Client.PostAsJsonAsync($"/api/contracts/{setup.ContractId}/employees", secondPosition);
        Assert.Equal(HttpStatusCode.Created, addPositionResponse.StatusCode);

        HttpResponseMessage response = await Client.GetAsync($"/api/contracts/{setup.ContractId}/timesheets?fromYear=2026&fromMonth=3&toYear=2026&toMonth=3");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        GetContractTimesheets.Response? payload = await response.Content.ReadFromJsonAsync<GetContractTimesheets.Response>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload!.Timesheets.Count());
        Assert.Contains(payload.Timesheets, timesheet => timesheet.Position == "Developer");
        Assert.Contains(payload.Timesheets, timesheet => timesheet.Position == "Koordinátor");
    }

    [Fact]
    public async Task GetContractTimesheets_ReturnsOnlyEmployeesAssignedToContract()
    {
        TestProjectSetup included = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), workload: 0.5m);
        TestProjectSetup excluded = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), workload: 0.5m);
        await ImportAttendanceAsync(included, 2024, 10);
        await ImportAttendanceAsync(excluded, 2024, 10);

        HttpResponseMessage response = await Client.GetAsync($"/api/contracts/{included.ContractId}/timesheets?fromYear=2024&fromMonth=10&toYear=2024&toMonth=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        GetContractTimesheets.Response? payload = await response.Content.ReadFromJsonAsync<GetContractTimesheets.Response>();
        GetContractTimesheets.TimesheetItem timesheet = Assert.Single(payload!.Timesheets);
        Assert.Equal(included.EmployeeId, timesheet.EmployeeId);
        Assert.DoesNotContain(payload.Employees, employee => employee.Id == excluded.EmployeeId);
    }

    private async Task ImportAttendanceAsync(TestProjectSetup setup, int year, int month, decimal workloadPercent = 50m)
    {
        byte[] file = AttendanceTimesheetTestFileBuilder.Create(setup.EmployeePersonalNumber, "Test Employee", year, month, workloadPercent);
        using MultipartFormDataContent form = TimesheetImportFormFactory.Create(setup.EmployeeId, file, "attendance.xlsx");
        HttpResponseMessage response = await Client.PostAsync("/api/timesheets/", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
