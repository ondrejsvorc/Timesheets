using System.Net;
using System.Net.Http.Json;
using Timesheets.Api.Contracts.Endpoints;
using Timesheets.Api.Data.Models;
using Timesheets.Api.Projects.Endpoints;

namespace Timesheets.Api.Tests.Integration;

internal sealed record TestProjectSetup(
    Guid ProjectId,
    Guid ContractId,
    Guid EmployeeId,
    Guid ContractEmployeeId,
    string EmployeePersonalNumber);

internal static class IntegrationTestDataFactory
{
    private static int _sequence;

    public static async Task<TestProjectSetup> CreateProjectWithPositionAsync(
        IServiceProvider services,
        HttpClient client,
        DateTime positionStart,
        DateTime? positionEnd = null,
        decimal workload = 1.0m)
    {
        int sequence = Interlocked.Increment(ref _sequence);
        string suffix = sequence.ToString(System.Globalization.CultureInfo.InvariantCulture);

        CreateProject.Request createProjectRequest = new(
            $"Test Project {suffix}",
            $"REG-TEST-{suffix}",
            positionStart,
            positionEnd?.AddYears(1));

        HttpResponseMessage projectResponse = await client.PostAsJsonAsync("/api/projects", createProjectRequest);
        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);
        CreateProject.Response? createdProject = await projectResponse.Content.ReadFromJsonAsync<CreateProject.Response>();
        Assert.NotNull(createdProject);
        Guid projectId = createdProject!.Project.Id;

        CreateProjectContract.Request createContractRequest = new($"Test Contract {suffix}", $"CONT-{suffix}");
        HttpResponseMessage contractResponse = await client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", createContractRequest);
        Assert.Equal(HttpStatusCode.Created, contractResponse.StatusCode);
        CreateProjectContract.Response? createdContract = await contractResponse.Content.ReadFromJsonAsync<CreateProjectContract.Response>();
        Assert.NotNull(createdContract);
        Guid contractId = createdContract!.ProjectContract.Id;

        string personalNumber = $"9{suffix.PadLeft(3, '0')}";
        Employee employee = await TestEmployeeFactory.CreateAsync(
            services,
            personalNumber,
            $"Test Employee {suffix}",
            $"test.employee.{suffix}@example.com");
        Guid employeeId = employee.Id;

        AddContractEmployee.Request addPositionRequest = new(
            employeeId,
            "POS-01",
            "Developer",
            workload,
            positionStart,
            positionEnd);

        HttpResponseMessage addPositionResponse = await client.PostAsJsonAsync($"/api/contracts/{contractId}/employees", addPositionRequest);
        Assert.Equal(HttpStatusCode.Created, addPositionResponse.StatusCode);

        HttpResponseMessage employeesResponse = await client.GetAsync($"/api/contracts/{contractId}/employees");
        Assert.Equal(HttpStatusCode.OK, employeesResponse.StatusCode);
        GetContractEmployees.Response? employees = await employeesResponse.Content.ReadFromJsonAsync<GetContractEmployees.Response>();
        Assert.NotNull(employees);

        GetContractEmployees.PositionItem position = employees!.Employees
            .Single(employeeItem => employeeItem.Id == employeeId)
            .Positions
            .Single(item => item.PositionCode == "POS-01");

        return new TestProjectSetup(projectId, contractId, employeeId, position.Id, personalNumber);
    }
}
