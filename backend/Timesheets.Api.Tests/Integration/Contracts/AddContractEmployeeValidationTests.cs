using System.Net;
using System.Net.Http.Json;
using Timesheets.Api.Contracts.Endpoints;
using Timesheets.Api.Projects.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Contracts;

public class AddContractEmployeeValidationTests : BaseIntegrationTest
{
    public AddContractEmployeeValidationTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<(Guid ContractId, Guid EmployeeId)> SetupContractAndEmployeeAsync()
    {
        CreateProject.Request projectRequest = new("AddEmp Proj", "P-1", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30));
        HttpResponseMessage projResp = await Client.PostAsJsonAsync("/api/projects", projectRequest);
        Guid projectId = (await projResp.Content.ReadFromJsonAsync<CreateProject.Response>())!.Project.Id;

        CreateProjectContract.Request contractRequest = new("AddEmp Cont", "C-1");
        HttpResponseMessage contResp = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", contractRequest);
        Guid contractId = (await contResp.Content.ReadFromJsonAsync<CreateProjectContract.Response>())!.ProjectContract.Id;
        Guid employeeId = await SeedEmployeeAsync("9998", "Jane AddContract", "jane@add.com");
        return (contractId, employeeId);
    }

    [Fact]
    public async Task AddContractEmployee_WithInvalidData_ReturnsBadRequest()
    {
        (Guid contractId, Guid employeeId) = await SetupContractAndEmployeeAsync();
        DateTime startDate = DateTime.UtcNow.Date;

        HttpResponseMessage emptyCodeResponse = await Client.PostAsJsonAsync($"/api/contracts/{contractId}/employees", new AddContractEmployee.Request(employeeId, "", "Dev", 1m, startDate, null));
        Assert.Equal(HttpStatusCode.BadRequest, emptyCodeResponse.StatusCode);

        HttpResponseMessage longCodeResponse = await Client.PostAsJsonAsync($"/api/contracts/{contractId}/employees", new AddContractEmployee.Request(employeeId, new string('A', 51), "Dev", 1m, startDate, null));
        Assert.Equal(HttpStatusCode.BadRequest, longCodeResponse.StatusCode);

        HttpResponseMessage emptyPositionResponse = await Client.PostAsJsonAsync($"/api/contracts/{contractId}/employees", new AddContractEmployee.Request(employeeId, "P1", "", 1m, startDate, null));
        Assert.Equal(HttpStatusCode.BadRequest, emptyPositionResponse.StatusCode);

        HttpResponseMessage longPositionResponse = await Client.PostAsJsonAsync($"/api/contracts/{contractId}/employees", new AddContractEmployee.Request(employeeId, "P1", new string('B', 201), 1m, startDate, null));
        Assert.Equal(HttpStatusCode.BadRequest, longPositionResponse.StatusCode);

        HttpResponseMessage invalidWorkloadResponse = await Client.PostAsJsonAsync($"/api/contracts/{contractId}/employees", new AddContractEmployee.Request(employeeId, "P1", "Dev", 0m, startDate, null));
        Assert.Equal(HttpStatusCode.BadRequest, invalidWorkloadResponse.StatusCode);

        HttpResponseMessage invalidDatesResponse = await Client.PostAsJsonAsync($"/api/contracts/{contractId}/employees", new AddContractEmployee.Request(employeeId, "P1", "Dev", 1m, startDate.AddDays(1), startDate));
        Assert.Equal(HttpStatusCode.BadRequest, invalidDatesResponse.StatusCode);
    }
}
