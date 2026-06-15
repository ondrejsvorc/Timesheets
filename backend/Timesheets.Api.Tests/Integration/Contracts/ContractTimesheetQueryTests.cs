using System.Net;
using System.Net.Http.Json;
using Timesheets.Api.Contracts.Endpoints;

namespace Timesheets.Api.Tests.Integration.Contracts;

public class ContractTimesheetQueryTests : BaseIntegrationTest
{
    public ContractTimesheetQueryTests(CustomWebApplicationFactory factory) : base(factory) { }

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

    private async Task ImportAttendanceAsync(TestProjectSetup setup, int year, int month)
    {
        byte[] file = AttendanceTimesheetTestFileBuilder.Create(setup.EmployeePersonalNumber, "Test Employee", year, month, 50m);
        using MultipartFormDataContent form = TimesheetImportFormFactory.Create(setup.EmployeeId, file, "attendance.xlsx");
        HttpResponseMessage response = await Client.PostAsync("/api/timesheets/", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
