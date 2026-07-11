using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;
using Timesheets.Api.Features.Employees.Endpoints;
using Timesheets.Api.Features.Timesheets.Endpoints;
using Xunit.Abstractions;

namespace Timesheets.Api.Tests.Integration.Performance;

public sealed class ExpectedLoadTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _output;

    public ExpectedLoadTests(CustomWebApplicationFactory factory, ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    [Fact]
    public async Task ExpectedLoad_HandlesSixHundredEmployeesAndTenProjectColumns()
    {
        (Guid employeeId, int year, int month) = await SeedExpectedLoadAsync();
        await Client.GetAsync("/api/employees");

        Stopwatch employeesTimer = Stopwatch.StartNew();
        HttpResponseMessage employeesResponse = await Client.GetAsync("/api/employees");
        employeesTimer.Stop();

        Stopwatch timesheetTimer = Stopwatch.StartNew();
        HttpResponseMessage timesheetResponse = await Client.GetAsync($"/api/timesheets/combined?employeeId={employeeId}&year={year}&month={month}");
        timesheetTimer.Stop();

        Assert.Equal(HttpStatusCode.OK, employeesResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, timesheetResponse.StatusCode);

        GetEmployees.Response? employees = await employeesResponse.Content.ReadFromJsonAsync<GetEmployees.Response>();
        GetCombinedTimesheet.Response? timesheet = await timesheetResponse.Content.ReadFromJsonAsync<GetCombinedTimesheet.Response>();
        Assert.NotNull(employees);
        Assert.NotNull(timesheet);
        Assert.True(employees!.Employees.Count() >= 600);
        Assert.Equal(10, timesheet!.Projects.Count());
        Assert.Equal(10, timesheet.Projects.Select(project => project.RegistrationNumber).Distinct().Count());
        Assert.Equal(DateTime.DaysInMonth(year, month), timesheet.Days.Count());
        Assert.True(employeesTimer.Elapsed < TimeSpan.FromSeconds(5), $"Načtení zaměstnanců trvalo {employeesTimer.Elapsed}.");
        Assert.True(timesheetTimer.Elapsed < TimeSpan.FromSeconds(5), $"Načtení kombinovaného výkazu trvalo {timesheetTimer.Elapsed}.");

        _output.WriteLine($"600 employees: {employeesTimer.ElapsedMilliseconds} ms");
        _output.WriteLine($"10 project columns: {timesheetTimer.ElapsedMilliseconds} ms");
    }

    private async Task<(Guid EmployeeId, int Year, int Month)> SeedExpectedLoadAsync()
    {
        const int year = 2037;
        const int month = 1;
        DateTime monthStart = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        List<Employee> employees = Enumerable.Range(1, 600)
            .Select(index => new Employee
            {
                Id = Guid.CreateVersion7(),
                EmployeeTypeId = TestEmployeeFactory.DefaultEmployeeTypeId,
                PersonalNumber = $"LOAD-{index:0000}",
                FirstName = "Load",
                Surname = $"Employee {index:0000}",
            })
            .ToList();
        Employee target = employees[0];
        dbContext.Employees.AddRange(employees);
        dbContext.EmployeeWorkloads.Add(new EmployeeWorkload { Id = Guid.CreateVersion7(), EmployeeId = target.Id, Year = year, Month = month, Workload = 1m });

        for (int index = 1; index <= 10; index++)
        {
            Guid contractId = Guid.CreateVersion7();
            Guid assignmentId = Guid.CreateVersion7();
            dbContext.Contracts.Add(new Contract { Id = contractId, ProjectId = SeededTestData.BetaProjectId, Name = $"Load Contract {index:00}", RegistrationNumber = $"LOAD-{index:00}" });
            dbContext.ContractEmployees.Add(new ContractEmployee { Id = assignmentId, ContractId = contractId, EmployeeId = target.Id, PositionCode = $"LOAD-{index:00}", Position = $"Load Position {index:00}", Workload = 0.05m, StartDate = monthStart, EndDate = monthStart.AddMonths(1).AddDays(-1) });
        }

        Guid timesheetId = Guid.CreateVersion7();
        TimesheetBootstrap.AddMonthWithDays(
            dbContext,
            new Data.Models.Timesheet
            {
                Id = timesheetId,
                EmployeeId = target.Id,
                TimesheetStatusId = TestTimesheetStatusIds.Draft,
                Year = year,
                Month = month,
            },
            TestEmployeeFactory.DefaultEmployeeTypeId,
            Enumerable.Range(1, DateTime.DaysInMonth(year, month)).Select(day =>
            {
                DateTime date = new(year, month, day, 0, 0, 0, DateTimeKind.Utc);
                return new AttendanceDay { Id = Guid.CreateVersion7(), Date = date, Workload = 1m, HoursObligation = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 0m : 8m, Schedules = "[]" };
            }));

        await dbContext.SaveChangesAsync();
        return (target.Id, year, month);
    }
}
