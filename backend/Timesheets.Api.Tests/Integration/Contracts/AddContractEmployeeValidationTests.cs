using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Timesheets.Api.Contracts.Endpoints;
using Timesheets.Api.Projects.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Contracts;

public class AddContractEmployeeValidationTests : BaseIntegrationTest
{
    public AddContractEmployeeValidationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<(Guid ContractId, Guid EmployeeId)> SetupContractAndEmployeeAsync()
    {
        var projectRequest = new CreateProject.Request("AddEmp Proj", "P-1", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30));
        var projResp = await Client.PostAsJsonAsync("/api/projects", projectRequest);
        var projectId = (await projResp.Content.ReadFromJsonAsync<CreateProject.Response>())!.Project.Id;

        var contractRequest = new CreateProjectContract.Request("AddEmp Cont", "C-1");
        var contResp = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", contractRequest);
        var contractId = (await contResp.Content.ReadFromJsonAsync<CreateProjectContract.Response>())!.ProjectContract.Id;

        Guid employeeId = await SeedEmployeeAsync("9998", "Jane AddContract", "jane@add.com");

        return (contractId, employeeId);
    }

    [Fact]
    public async Task AddContractEmployee_WithInvalidData_ReturnsBadRequest()
    {
        var (contractId, employeeId) = await SetupContractAndEmployeeAsync();

        // Empty PositionCode
        var req1 = new AddContractEmployee.Request(employeeId, "", "Dev", 1m, DateTime.UtcNow.Date, null);
        var res1 = await Client.PostAsJsonAsync($"/api/contracts/{contractId}/employees", req1);
        Assert.Equal(HttpStatusCode.BadRequest, res1.StatusCode);

        // Long PositionCode
        var req2 = new AddContractEmployee.Request(employeeId, new string('A', 51), "Dev", 1m, DateTime.UtcNow.Date, null);
        var res2 = await Client.PostAsJsonAsync($"/api/contracts/{contractId}/employees", req2);
        Assert.Equal(HttpStatusCode.BadRequest, res2.StatusCode);

        // Empty Position
        var req3 = new AddContractEmployee.Request(employeeId, "P1", "", 1m, DateTime.UtcNow.Date, null);
        var res3 = await Client.PostAsJsonAsync($"/api/contracts/{contractId}/employees", req3);
        Assert.Equal(HttpStatusCode.BadRequest, res3.StatusCode);

        // Long Position
        var req4 = new AddContractEmployee.Request(employeeId, "P1", new string('B', 201), 1m, DateTime.UtcNow.Date, null);
        var res4 = await Client.PostAsJsonAsync($"/api/contracts/{contractId}/employees", req4);
        Assert.Equal(HttpStatusCode.BadRequest, res4.StatusCode);

        // Invalid Workload
        var req5 = new AddContractEmployee.Request(employeeId, "P1", "Dev", 0m, DateTime.UtcNow.Date, null);
        var res5 = await Client.PostAsJsonAsync($"/api/contracts/{contractId}/employees", req5);
        Assert.Equal(HttpStatusCode.BadRequest, res5.StatusCode);

        // StartDate >= EndDate
        var req6 = new AddContractEmployee.Request(employeeId, "P1", "Dev", 1m, DateTime.UtcNow.Date.AddDays(1), DateTime.UtcNow.Date);
        var res6 = await Client.PostAsJsonAsync($"/api/contracts/{contractId}/employees", req6);
        Assert.Equal(HttpStatusCode.BadRequest, res6.StatusCode);
    }
}
