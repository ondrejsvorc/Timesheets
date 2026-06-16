using System.Net;
using System.Net.Http.Json;
using Timesheets.Api.Contracts.Endpoints;
using Timesheets.Api.Projects.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Contracts;

public class AddContractEmployeeValidationTests : BaseIntegrationTest
{
    public AddContractEmployeeValidationTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<(Guid ContractId, Guid EmployeeId, DateTime ProjectStart, DateTime ProjectEnd)> SetupContractAndEmployeeAsync()
    {
        string suffix = Guid.NewGuid().ToString("N")[..8];
        DateTime projectStart = DateTime.UtcNow.Date;
        DateTime projectEnd = projectStart.AddDays(30);
        CreateProject.Request projectRequest = new($"AddEmp Proj {suffix}", $"P-{suffix}", projectStart, projectEnd);
        HttpResponseMessage projResp = await Client.PostAsJsonAsync("/api/projects", projectRequest);
        projResp.EnsureSuccessStatusCode();
        Guid projectId = (await projResp.Content.ReadFromJsonAsync<CreateProject.Response>())!.Project.Id;

        CreateProjectContract.Request contractRequest = new($"AddEmp Cont {suffix}", $"C-{suffix}");
        HttpResponseMessage contResp = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", contractRequest);
        contResp.EnsureSuccessStatusCode();
        Guid contractId = (await contResp.Content.ReadFromJsonAsync<CreateProjectContract.Response>())!.ProjectContract.Id;
        Guid employeeId = await SeedEmployeeAsync($"A{suffix}", $"Jane AddContract {suffix}");
        return (contractId, employeeId, projectStart, projectEnd);
    }

    [Fact]
    public async Task AddContractEmployee_WithInvalidData_ReturnsBadRequest()
    {
        (Guid contractId, Guid employeeId, _, _) = await SetupContractAndEmployeeAsync();
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

    [Fact]
    public async Task AddContractEmployee_OutsideProjectRange_ReturnsBadRequest()
    {
        (Guid contractId, Guid employeeId, DateTime projectStart, DateTime projectEnd) = await SetupContractAndEmployeeAsync();

        HttpResponseMessage beforeProject = await Client.PostAsJsonAsync($"/api/contracts/{contractId}/employees", new AddContractEmployee.Request(employeeId, "P1", "Dev", 1m, projectStart.AddDays(-1), projectEnd));
        HttpResponseMessage openAssignment = await Client.PostAsJsonAsync($"/api/contracts/{contractId}/employees", new AddContractEmployee.Request(employeeId, "P1", "Dev", 1m, projectStart, null));
        HttpResponseMessage endAfterProject = await Client.PostAsJsonAsync($"/api/contracts/{contractId}/employees", new AddContractEmployee.Request(employeeId, "P2", "Dev", 1m, projectStart, projectEnd.AddDays(1)));

        Assert.Equal(HttpStatusCode.BadRequest, beforeProject.StatusCode);
        Assert.Equal(HttpStatusCode.Created, openAssignment.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, endAfterProject.StatusCode);
    }
}
