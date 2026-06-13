using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Timesheets.Api.Contracts.Endpoints;
using Timesheets.Api.Projects.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Contracts;

public class UpdateContractEmployeeValidationTests : BaseIntegrationTest
{
    public UpdateContractEmployeeValidationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<(Guid ContractId, Guid ContractEmployeeId)> SetupContractEmployeeAsync()
    {
        var projectRequest = new CreateProject.Request("UpdEmp Proj", "P-2", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30));
        var projResp = await Client.PostAsJsonAsync("/api/projects", projectRequest);
        var projectId = (await projResp.Content.ReadFromJsonAsync<CreateProject.Response>())!.Project.Id;

        var contractRequest = new CreateProjectContract.Request("UpdEmp Cont", "C-2");
        var contResp = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", contractRequest);
        var contractId = (await contResp.Content.ReadFromJsonAsync<CreateProjectContract.Response>())!.ProjectContract.Id;

        Guid employeeId = await SeedEmployeeAsync("9997", "John UpdContract", "john@upd.com");

        var addRequest = new AddContractEmployee.Request(employeeId, "P1", "Dev", 1m, DateTime.UtcNow.Date, null);
        var addResp = await Client.PostAsJsonAsync($"/api/contracts/{contractId}/employees", addRequest);
        var contractEmployeeId = (await addResp.Content.ReadFromJsonAsync<AddContractEmployee.Response>())!.EmployeeId; // wait, Response returns EmployeeId but not ContractEmployeeId directly? Let's check AddContractEmployee Response.

        // Wait, AddContractEmployee returns response with EmployeeId, not ContractEmployeeId. 
        // We can get it via GET /api/contracts/{contractId}/employees
        var getEmployeesResponse = await Client.GetAsync($"/api/contracts/{contractId}/employees");
        var employeesList = await getEmployeesResponse.Content.ReadFromJsonAsync<GetContractEmployees.Response>();
        var positionId = employeesList!.Employees.First().Positions.First().Id;

        return (contractId, positionId);
    }

    [Fact]
    public async Task UpdateContractEmployee_WithInvalidData_ReturnsBadRequest()
    {
        var (contractId, contractEmployeeId) = await SetupContractEmployeeAsync();

        // Empty PositionCode
        var req1 = new UpdateContractEmployee.Request("", "Dev", 1m, DateTime.UtcNow.Date, null);
        var res1 = await Client.PutAsJsonAsync($"/api/contracts/{contractId}/employees/{contractEmployeeId}", req1);
        Assert.Equal(HttpStatusCode.BadRequest, res1.StatusCode);

        // Long PositionCode
        var req2 = new UpdateContractEmployee.Request(new string('A', 51), "Dev", 1m, DateTime.UtcNow.Date, null);
        var res2 = await Client.PutAsJsonAsync($"/api/contracts/{contractId}/employees/{contractEmployeeId}", req2);
        Assert.Equal(HttpStatusCode.BadRequest, res2.StatusCode);

        // Empty Position
        var req3 = new UpdateContractEmployee.Request("P1", "", 1m, DateTime.UtcNow.Date, null);
        var res3 = await Client.PutAsJsonAsync($"/api/contracts/{contractId}/employees/{contractEmployeeId}", req3);
        Assert.Equal(HttpStatusCode.BadRequest, res3.StatusCode);

        // Long Position
        var req4 = new UpdateContractEmployee.Request("P1", new string('B', 201), 1m, DateTime.UtcNow.Date, null);
        var res4 = await Client.PutAsJsonAsync($"/api/contracts/{contractId}/employees/{contractEmployeeId}", req4);
        Assert.Equal(HttpStatusCode.BadRequest, res4.StatusCode);

        // Invalid Workload
        var req5 = new UpdateContractEmployee.Request("P1", "Dev", 0m, DateTime.UtcNow.Date, null);
        var res5 = await Client.PutAsJsonAsync($"/api/contracts/{contractId}/employees/{contractEmployeeId}", req5);
        Assert.Equal(HttpStatusCode.BadRequest, res5.StatusCode);

        // StartDate >= EndDate
        var req6 = new UpdateContractEmployee.Request("P1", "Dev", 1m, DateTime.UtcNow.Date.AddDays(1), DateTime.UtcNow.Date);
        var res6 = await Client.PutAsJsonAsync($"/api/contracts/{contractId}/employees/{contractEmployeeId}", req6);
        Assert.Equal(HttpStatusCode.BadRequest, res6.StatusCode);
    }
}
