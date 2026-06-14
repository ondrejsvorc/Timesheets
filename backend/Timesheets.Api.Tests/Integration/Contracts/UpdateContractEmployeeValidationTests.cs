using System.Net;
using System.Net.Http.Json;
using Timesheets.Api.Contracts.Endpoints;
using Timesheets.Api.Projects.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Contracts;

public class UpdateContractEmployeeValidationTests : BaseIntegrationTest
{
    public UpdateContractEmployeeValidationTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<(Guid ContractId, Guid ContractEmployeeId)> SetupContractEmployeeAsync()
    {
        CreateProject.Request projectRequest = new("UpdEmp Proj", "P-2", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30));
        HttpResponseMessage projResp = await Client.PostAsJsonAsync("/api/projects", projectRequest);
        Guid projectId = (await projResp.Content.ReadFromJsonAsync<CreateProject.Response>())!.Project.Id;

        CreateProjectContract.Request contractRequest = new("UpdEmp Cont", "C-2");
        HttpResponseMessage contResp = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", contractRequest);
        Guid contractId = (await contResp.Content.ReadFromJsonAsync<CreateProjectContract.Response>())!.ProjectContract.Id;
        Guid employeeId = await SeedEmployeeAsync("9997", "John UpdContract", "john@upd.com");

        AddContractEmployee.Request addRequest = new(employeeId, "P1", "Dev", 1m, DateTime.UtcNow.Date, null);
        await Client.PostAsJsonAsync($"/api/contracts/{contractId}/employees", addRequest);

        GetContractEmployees.Response? employeesList = await (await Client.GetAsync($"/api/contracts/{contractId}/employees")).Content.ReadFromJsonAsync<GetContractEmployees.Response>();
        Guid positionId = employeesList!.Employees.First().Positions.First().Id;
        return (contractId, positionId);
    }

    [Fact]
    public async Task UpdateContractEmployee_WithInvalidData_ReturnsBadRequest()
    {
        (Guid contractId, Guid contractEmployeeId) = await SetupContractEmployeeAsync();
        DateTime startDate = DateTime.UtcNow.Date;
        string url = $"/api/contracts/{contractId}/employees/{contractEmployeeId}";

        HttpResponseMessage emptyCodeResponse = await Client.PutAsJsonAsync(url, new UpdateContractEmployee.Request("", "Dev", 1m, startDate, null));
        Assert.Equal(HttpStatusCode.BadRequest, emptyCodeResponse.StatusCode);

        HttpResponseMessage longCodeResponse = await Client.PutAsJsonAsync(url, new UpdateContractEmployee.Request(new string('A', 51), "Dev", 1m, startDate, null));
        Assert.Equal(HttpStatusCode.BadRequest, longCodeResponse.StatusCode);

        HttpResponseMessage emptyPositionResponse = await Client.PutAsJsonAsync(url, new UpdateContractEmployee.Request("P1", "", 1m, startDate, null));
        Assert.Equal(HttpStatusCode.BadRequest, emptyPositionResponse.StatusCode);

        HttpResponseMessage longPositionResponse = await Client.PutAsJsonAsync(url, new UpdateContractEmployee.Request("P1", new string('B', 201), 1m, startDate, null));
        Assert.Equal(HttpStatusCode.BadRequest, longPositionResponse.StatusCode);

        HttpResponseMessage invalidWorkloadResponse = await Client.PutAsJsonAsync(url, new UpdateContractEmployee.Request("P1", "Dev", 0m, startDate, null));
        Assert.Equal(HttpStatusCode.BadRequest, invalidWorkloadResponse.StatusCode);

        HttpResponseMessage invalidDatesResponse = await Client.PutAsJsonAsync(url, new UpdateContractEmployee.Request("P1", "Dev", 1m, startDate.AddDays(1), startDate));
        Assert.Equal(HttpStatusCode.BadRequest, invalidDatesResponse.StatusCode);
    }
}
