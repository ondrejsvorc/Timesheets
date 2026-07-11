using System.Net;
using System.Net.Http.Json;
using Timesheets.Api.Features.Contracts.Endpoints;
using Timesheets.Api.Features.Projects.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Contracts;

public class UpdateContractEmployeeValidationTests : BaseIntegrationTest
{
    private static int _identifierSequence = 2100;

    public UpdateContractEmployeeValidationTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<(Guid ContractId, Guid ContractEmployeeId, DateTime ProjectEnd)> SetupContractEmployeeAsync()
    {
        string suffix = TestIdentifiers.Suffix();
        int identifier = Interlocked.Increment(ref _identifierSequence);
        DateTime projectStart = DateTime.UtcNow.Date;
        DateTime projectEnd = projectStart.AddDays(30);
        CreateProject.Request projectRequest = new($"UpdEmp Proj {suffix}", TestIdentifiers.Project(identifier), projectStart, projectEnd);
        HttpResponseMessage projResp = await Client.PostAsJsonAsync("/api/projects", projectRequest);
        projResp.EnsureSuccessStatusCode();
        Guid contractEmployeeId = (await projResp.Content.ReadFromJsonAsync<CreateProject.Response>())!.Project.Id;

        CreateProjectContract.Request contractRequest = new($"UpdEmp Cont {suffix}", TestIdentifiers.Contract(identifier));
        HttpResponseMessage contResp = await Client.PostAsJsonAsync($"/api/projects/{contractEmployeeId}/contracts", contractRequest);
        contResp.EnsureSuccessStatusCode();
        Guid contractId = (await contResp.Content.ReadFromJsonAsync<CreateProjectContract.Response>())!.ProjectContract.Id;
        Guid employeeId = await SeedEmployeeAsync($"U{suffix}", "John", $"UpdContract {suffix}");

        AddContractEmployee.Request addRequest = new(employeeId, TestIdentifiers.Position(1), "Dev", 1m, projectStart, projectEnd);
        HttpResponseMessage addResponse = await Client.PostAsJsonAsync($"/api/contracts/{contractId}/employees", addRequest);
        addResponse.EnsureSuccessStatusCode();

        GetContractEmployees.Response? employeesList = await (await Client.GetAsync($"/api/contracts/{contractId}/employees")).Content.ReadFromJsonAsync<GetContractEmployees.Response>();
        Guid positionId = employeesList!.Employees.First().Positions.First().Id;
        return (contractId, positionId, projectEnd);
    }

    [Fact]
    public async Task UpdateContractEmployee_WithInvalidData_ReturnsBadRequest()
    {
        (Guid contractId, Guid contractEmployeeId, _) = await SetupContractEmployeeAsync();
        DateTime startDate = DateTime.UtcNow.Date;
        string url = $"/api/contracts/{contractId}/employees/{contractEmployeeId}";

        HttpResponseMessage emptyCodeResponse = await Client.PutAsJsonAsync(url, new UpdateContractEmployee.Request("", "Dev", 1m, startDate, null));
        Assert.Equal(HttpStatusCode.BadRequest, emptyCodeResponse.StatusCode);

        HttpResponseMessage longCodeResponse = await Client.PutAsJsonAsync(url, new UpdateContractEmployee.Request(new string('A', 51), "Dev", 1m, startDate, null));
        Assert.Equal(HttpStatusCode.BadRequest, longCodeResponse.StatusCode);

        HttpResponseMessage emptyPositionResponse = await Client.PutAsJsonAsync(url, new UpdateContractEmployee.Request(TestIdentifiers.Position(1), "", 1m, startDate, null));
        Assert.Equal(HttpStatusCode.BadRequest, emptyPositionResponse.StatusCode);

        HttpResponseMessage longPositionResponse = await Client.PutAsJsonAsync(url, new UpdateContractEmployee.Request(TestIdentifiers.Position(1), new string('B', 201), 1m, startDate, null));
        Assert.Equal(HttpStatusCode.BadRequest, longPositionResponse.StatusCode);

        HttpResponseMessage invalidWorkloadResponse = await Client.PutAsJsonAsync(url, new UpdateContractEmployee.Request(TestIdentifiers.Position(1), "Dev", 0m, startDate, null));
        Assert.Equal(HttpStatusCode.BadRequest, invalidWorkloadResponse.StatusCode);

        HttpResponseMessage invalidDatesResponse = await Client.PutAsJsonAsync(url, new UpdateContractEmployee.Request(TestIdentifiers.Position(1), "Dev", 1m, startDate.AddDays(1), startDate));
        Assert.Equal(HttpStatusCode.BadRequest, invalidDatesResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateContractEmployee_BeyondProjectEnd_ReturnsBadRequest()
    {
        (Guid contractId, Guid contractEmployeeId, DateTime projectEnd) = await SetupContractEmployeeAsync();
        UpdateContractEmployee.Request request = new(TestIdentifiers.Position(1), "Dev", 1m, DateTime.UtcNow.Date, projectEnd.AddDays(1));

        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/contracts/{contractId}/employees/{contractEmployeeId}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
