using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Timesheets.Api.Contracts.Endpoints;
using Timesheets.Api.Employees.Endpoints;
using Timesheets.Api.Projects.Endpoints;
using Timesheets.Api.Tests.Integration;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Contracts;

public class ContractEmployeeLifecycleTests : BaseIntegrationTest
{
    public ContractEmployeeLifecycleTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Contract_Employee_Lifecycle_HappyPath_CompletesSuccessfully()
    {
        // 1. Setup: Create Project
        var createProjectRequest = new CreateProject.Request(
            "Test Project for Contract Employees",
            "REG-CON-EMP-001",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(30)
        );
        var projectResponse = await Client.PostAsJsonAsync("/api/projects", createProjectRequest);
        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);
        var createdProject = await projectResponse.Content.ReadFromJsonAsync<CreateProject.Response>();
        var projectId = createdProject!.Project.Id;

        // 2. Setup: Create Contract
        var createContractRequest = new CreateProjectContract.Request(
            "Test Contract Employee",
            "REG-CONT-001"
        );
        var contractResponse = await Client.PostAsJsonAsync($"/api/projects/{projectId}/contracts", createContractRequest);
        Assert.Equal(HttpStatusCode.Created, contractResponse.StatusCode);
        var createdContract = await contractResponse.Content.ReadFromJsonAsync<CreateProjectContract.Response>();
        var contractId = createdContract!.ProjectContract.Id;

        // 3. Setup: Create Employee
        var createEmployeeRequest = new CreateEmployee.Request(
            Guid.NewGuid(), // EmployeeTypeId might need to be valid or we can pass a random guid if FK doesn't constrain it strictly
            9999,
            "John Doe Contract",
            "john.doe@contracts.com",
            false
        );
        var employeeResponse = await Client.PostAsJsonAsync("/api/employees", createEmployeeRequest);
        // If Employee creation requires a real EmployeeType from the DB seed, this might fail with 500 or 400. 
        // We'll proceed with this and fix if it fails during execution check.
        Assert.Equal(HttpStatusCode.Created, employeeResponse.StatusCode);
        var createdEmployee = await employeeResponse.Content.ReadFromJsonAsync<CreateEmployee.Response>();
        var employeeId = createdEmployee!.Id;

        // 4. POST /api/contracts/{id}/employees
        var addEmployeeRequest = new AddContractEmployee.Request(
            employeeId,
            "POS-01",
            "Developer",
            1.0m,
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(15)
        );
        var addResponse = await Client.PostAsJsonAsync($"/api/contracts/{contractId}/employees", addEmployeeRequest);
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        // 5. GET /api/contracts/{id}/employees
        var getEmployeesResponse = await Client.GetAsync($"/api/contracts/{contractId}/employees");
        Assert.Equal(HttpStatusCode.OK, getEmployeesResponse.StatusCode);
        var employeesList = await getEmployeesResponse.Content.ReadFromJsonAsync<GetContractEmployees.Response>();
        Assert.NotNull(employeesList);
        Assert.Contains(employeesList!.Employees, ce => ce.Id == employeeId && ce.Positions.Any(p => p.PositionCode == "POS-01"));

        var createdPosition = employeesList.Employees.First(ce => ce.Id == employeeId).Positions.First(p => p.PositionCode == "POS-01");
        var contractEmployeeId = createdPosition.Id;

        // 6. DELETE /api/contracts/{id}/employees/{contractEmployeeId}
        var deleteResponse = await Client.DeleteAsync($"/api/contracts/{contractId}/employees/{contractEmployeeId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // 7. Verify Employee is removed
        var getEmployeesAfterDeleteResponse = await Client.GetAsync($"/api/contracts/{contractId}/employees");
        var employeesListAfterDelete = await getEmployeesAfterDeleteResponse.Content.ReadFromJsonAsync<GetContractEmployees.Response>();
        Assert.DoesNotContain(employeesListAfterDelete!.Employees, ce => ce.Id == employeeId && ce.Positions.Any(p => p.Id == contractEmployeeId));
    }
}
