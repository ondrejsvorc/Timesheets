using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;
using Timesheets.Api.Timesheets.Endpoints;

namespace Timesheets.Api.Tests.Integration.Timesheets;

public class TimesheetStatusActionTests : BaseIntegrationTest
{
    public TimesheetStatusActionTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task ApproveProject_UsesActionInsteadOfStatusId()
    {
        using (IServiceScope setupScope = CreateScope())
        {
            AppDbContext setupContext = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            AttendanceTimesheet setupAttendance = await setupContext.AttendanceTimesheets.SingleAsync(timesheet => timesheet.Id == SeededTestData.PetrDecAttendanceTimesheetId);
            ProjectTimesheet setupProject = await setupContext.ProjectTimesheets.SingleAsync(timesheet => timesheet.Id == SeededTestData.PetrDecProjectTimesheetId);
            setupAttendance.TimesheetStatusId = TestTimesheetStatusIds.Submitted;
            setupProject.TimesheetStatusId = TestTimesheetStatusIds.Draft;
            setupProject.LockedAt = null;
            setupProject.LockedBy = null;
            await setupContext.SaveChangesAsync();
        }

        UpdateCombinedTimesheetStatus.Request request = new(SeededTestData.PetrEmployeeId, 2024, 12, "approve", null, [SeededTestData.PetrDecProjectTimesheetId]);

        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/timesheets/combined/status", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var project = await dbContext.ProjectTimesheets.Where(timesheet => timesheet.Id == SeededTestData.PetrDecProjectTimesheetId).Select(timesheet => new { timesheet.TimesheetStatusId, timesheet.LockedAt, timesheet.LockedBy }).SingleAsync();
        Assert.Equal(TestTimesheetStatusIds.Approved, project.TimesheetStatusId);
        Assert.NotNull(project.LockedAt);
        Assert.NotNull(project.LockedBy);
    }

    [Fact]
    public async Task ApproveProject_RequiresSubmittedWholeTimesheet()
    {
        using (IServiceScope setupScope = CreateScope())
        {
            AppDbContext setupContext = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            AttendanceTimesheet attendance = await setupContext.AttendanceTimesheets.SingleAsync(timesheet => timesheet.Id == SeededTestData.PetrDecAttendanceTimesheetId);
            ProjectTimesheet project = await setupContext.ProjectTimesheets.SingleAsync(timesheet => timesheet.Id == SeededTestData.PetrDecProjectTimesheetId);
            attendance.TimesheetStatusId = TestTimesheetStatusIds.Draft;
            project.TimesheetStatusId = TestTimesheetStatusIds.Draft;
            project.LockedAt = null;
            project.LockedBy = null;
            await setupContext.SaveChangesAsync();
        }

        UpdateCombinedTimesheetStatus.Request request = new(SeededTestData.PetrEmployeeId, 2024, 12, "approve", null, [SeededTestData.PetrDecProjectTimesheetId]);
        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/timesheets/combined/status", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReturnProject_ReopensWholeTimesheet()
    {
        using (IServiceScope setupScope = CreateScope())
        {
            AppDbContext setupContext = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            AttendanceTimesheet setupAttendance = await setupContext.AttendanceTimesheets.SingleAsync(timesheet => timesheet.Id == SeededTestData.PetrDecAttendanceTimesheetId);
            ProjectTimesheet setupProject = await setupContext.ProjectTimesheets.SingleAsync(timesheet => timesheet.Id == SeededTestData.PetrDecProjectTimesheetId);
            setupAttendance.TimesheetStatusId = TestTimesheetStatusIds.Submitted;
            setupProject.TimesheetStatusId = TestTimesheetStatusIds.Approved;
            setupProject.LockedAt = DateTime.UtcNow;
            setupProject.LockedBy = SeededTestData.JanNovakEmployeeId;
            await setupContext.SaveChangesAsync();
        }

        UpdateCombinedTimesheetStatus.Request request = new(SeededTestData.PetrEmployeeId, 2024, 12, "return", "Opravit hodiny.", [SeededTestData.PetrDecProjectTimesheetId]);
        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/timesheets/combined/status", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using IServiceScope assertionScope = CreateScope();
        AppDbContext assertionContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        AttendanceTimesheet attendance = await assertionContext.AttendanceTimesheets.AsNoTracking().SingleAsync(timesheet => timesheet.Id == SeededTestData.PetrDecAttendanceTimesheetId);
        ProjectTimesheet project = await assertionContext.ProjectTimesheets.AsNoTracking().SingleAsync(timesheet => timesheet.Id == SeededTestData.PetrDecProjectTimesheetId);
        Assert.Equal(TestTimesheetStatusIds.Draft, attendance.TimesheetStatusId);
        Assert.Equal(TestTimesheetStatusIds.Draft, project.TimesheetStatusId);
        Assert.Null(project.LockedAt);
        Assert.Null(project.LockedBy);
    }

    [Fact]
    public async Task SubmitWhole_NotifiesManagers()
    {
        const int year = 2039;
        const int month = 1;
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid contractEmployeeId = Guid.CreateVersion7();
        Guid projectTimesheetId = Guid.CreateVersion7();

        using (IServiceScope scope = CreateScope())
        {
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            DateTime periodStart = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            AttendanceDay[] days = Enumerable.Range(1, DateTime.DaysInMonth(year, month))
                .Select(day => new AttendanceDay { Id = Guid.CreateVersion7(), Date = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc), Workload = 0m, HoursObligation = 0m, Schedules = "[]" })
                .ToArray();

            dbContext.ContractManagers.Add(new ContractManager { Id = Guid.CreateVersion7(), ContractId = SeededTestData.AlphaContractId, EmployeeId = SeededTestData.MarieEmployeeId });
            dbContext.ContractEmployees.Add(new ContractEmployee { Id = contractEmployeeId, ContractId = SeededTestData.AlphaContractId, EmployeeId = SeededTestData.JanNovakEmployeeId, PositionCode = "WF-2039", Position = "Workflow 2039", Workload = 0m, StartDate = periodStart, EndDate = periodStart.AddMonths(1).AddDays(-1) });
            dbContext.EmployeeWorkloads.Add(new EmployeeWorkload { Id = Guid.CreateVersion7(), EmployeeId = SeededTestData.JanNovakEmployeeId, Year = year, Month = month, Workload = 0m });
            dbContext.AttendanceTimesheets.Add(new AttendanceTimesheet { Id = attendanceTimesheetId, EmployeeId = SeededTestData.JanNovakEmployeeId, TimesheetStatusId = TestTimesheetStatusIds.Draft, Year = year, Month = month, Days = days });
            dbContext.ProjectTimesheets.Add(new ProjectTimesheet { Id = projectTimesheetId, EmployeeId = SeededTestData.JanNovakEmployeeId, ContractId = SeededTestData.AlphaContractId, ContractEmployeeId = contractEmployeeId, TimesheetStatusId = TestTimesheetStatusIds.Draft, Year = year, Month = month, Workload = 0m });
            await dbContext.SaveChangesAsync();
        }

        UpdateCombinedTimesheetStatus.Request request = new(SeededTestData.JanNovakEmployeeId, year, month, "submit", null, [attendanceTimesheetId]);
        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/timesheets/combined/status", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using IServiceScope assertionScope = CreateScope();
        AppDbContext assertionContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Guid statusId = await assertionContext.AttendanceTimesheets.Where(timesheet => timesheet.Id == attendanceTimesheetId).Select(timesheet => timesheet.TimesheetStatusId).SingleAsync();
        Notification notification = await assertionContext.Notifications.AsNoTracking().SingleAsync(item => item.EmployeeId == SeededTestData.MarieEmployeeId && item.Message.Contains("2039"));
        Assert.Equal(TestTimesheetStatusIds.Submitted, statusId);
        Assert.Contains("Jan Novák", notification.Message);
    }

    [Fact]
    public async Task ContractManager_CannotSubmitVisibleEmployeesWholeTimesheet()
    {
        const int year = 2040;
        const int month = 1;
        DateTime periodStart = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid contractEmployeeId = Guid.CreateVersion7();
        Employee employee = await TestEmployeeFactory.CreateAsync(Factory.Services, "emp-" + TestIdentifiers.Suffix(17), "Visible Employee");
        Employee manager = await TestEmployeeFactory.CreateAsync(Factory.Services, "cm-" + TestIdentifiers.Suffix(17), "Contract Manager");

        using (IServiceScope scope = CreateScope())
        {
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.ContractManagers.Add(new ContractManager { Id = Guid.CreateVersion7(), ContractId = SeededTestData.AlphaContractId, EmployeeId = manager.Id });
            dbContext.ContractEmployees.Add(new ContractEmployee { Id = contractEmployeeId, ContractId = SeededTestData.AlphaContractId, EmployeeId = employee.Id, PositionCode = "WF-2040", Position = "Workflow 2040", Workload = 0m, StartDate = periodStart, EndDate = periodStart.AddMonths(1).AddDays(-1) });
            dbContext.EmployeeWorkloads.Add(new EmployeeWorkload { Id = Guid.CreateVersion7(), EmployeeId = employee.Id, Year = year, Month = month, Workload = 0m });
            dbContext.AttendanceTimesheets.Add(new AttendanceTimesheet { Id = attendanceTimesheetId, EmployeeId = employee.Id, TimesheetStatusId = TestTimesheetStatusIds.Draft, Year = year, Month = month });
            await dbContext.SaveChangesAsync();
        }

        UpdateCombinedTimesheetStatus.Request request = new(employee.Id, year, month, "submit", null, [attendanceTimesheetId]);
        HttpStatusCode statusCode = await PutStatusAsAsync(manager.PersonalNumber, request);

        Assert.Equal(HttpStatusCode.Unauthorized, statusCode);
        using IServiceScope assertionScope = CreateScope();
        AppDbContext assertionContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Guid statusId = await assertionContext.AttendanceTimesheets.Where(timesheet => timesheet.Id == attendanceTimesheetId).Select(timesheet => timesheet.TimesheetStatusId).SingleAsync();
        Assert.Equal(TestTimesheetStatusIds.Draft, statusId);
    }

    [Fact]
    public async Task UnsupportedAction_ReturnsBadRequest()
    {
        UpdateCombinedTimesheetStatus.Request request = new(SeededTestData.PetrEmployeeId, 2024, 12, "delete", null, [SeededTestData.PetrDecProjectTimesheetId]);

        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/timesheets/combined/status", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GlobalManager_CanChangeAnotherEmployeesWholeStatus()
    {
        // Approval of the whole (attendance) timesheet still requires all project parts to be approved.
        using (IServiceScope setupScope = CreateScope())
        {
            AppDbContext setupContext = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            AttendanceTimesheet setupAttendance = await setupContext.AttendanceTimesheets.SingleAsync(timesheet => timesheet.Id == Guid.Parse("70000000-0000-0000-0000-000000000002"));
            setupAttendance.TimesheetStatusId = TestTimesheetStatusIds.Submitted;

            List<ProjectTimesheet> projects = await setupContext.ProjectTimesheets
                .Where(t => t.EmployeeId == SeededTestData.MarieEmployeeId && t.Year == 2024 && t.Month == 12)
                .ToListAsync();
            foreach (ProjectTimesheet project in projects)
            {
                project.TimesheetStatusId = TestTimesheetStatusIds.Approved;
                project.LockedAt = DateTime.UtcNow;
                project.LockedBy = SeededTestData.JanNovakEmployeeId;
            }

            await setupContext.SaveChangesAsync();
        }

        UpdateCombinedTimesheetStatus.Request request = new(SeededTestData.MarieEmployeeId, 2024, 12, "approve", null, [Guid.Parse("70000000-0000-0000-0000-000000000002")]);

        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/timesheets/combined/status", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Employee_CanCompleteOwnWholeTimesheet()
    {
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        using (IServiceScope scope = CreateScope())
        {
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.AttendanceTimesheets.Add(new AttendanceTimesheet { Id = attendanceTimesheetId, EmployeeId = SeededTestData.JanNovakEmployeeId, TimesheetStatusId = TestTimesheetStatusIds.Submitted, Year = 2099, Month = 1 });
            await dbContext.SaveChangesAsync();
        }

        UpdateCombinedTimesheetStatus.Request request = new(SeededTestData.JanNovakEmployeeId, 2099, 1, "approve", null, [attendanceTimesheetId]);
        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/timesheets/combined/status", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using IServiceScope assertionScope = CreateScope();
        AppDbContext assertionContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Guid statusId = await assertionContext.AttendanceTimesheets.Where(timesheet => timesheet.Id == attendanceTimesheetId).Select(timesheet => timesheet.TimesheetStatusId).SingleAsync();
        Assert.Equal(TestTimesheetStatusIds.Approved, statusId);
    }
    [Fact]
    public async Task ProjectManager_CanApproveManagedProjectPart()
    {
        WorkflowSetup workflow = await CreateWorkflowSetupAsync(TestTimesheetStatusIds.Submitted, TestTimesheetStatusIds.Draft);
        string managerPersonalNumber = "pm-" + TestIdentifiers.Suffix(17);
        Employee manager = await TestEmployeeFactory.CreateAsync(Factory.Services, managerPersonalNumber, "Project Manager");
        using (IServiceScope scope = CreateScope())
        {
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.ProjectManagers.Add(new ProjectManager { Id = Guid.CreateVersion7(), ProjectId = workflow.ProjectId, EmployeeId = manager.Id });
            await dbContext.SaveChangesAsync();
        }

        UpdateCombinedTimesheetStatus.Request request = new(workflow.EmployeeId, workflow.Year, workflow.Month, "approve", null, [workflow.ProjectTimesheetId]);
        HttpStatusCode statusCode = await PutStatusAsAsync(manager.PersonalNumber, request);

        Assert.Equal(HttpStatusCode.OK, statusCode);
        using IServiceScope assertionScope = CreateScope();
        AppDbContext assertionContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        ProjectTimesheet project = await assertionContext.ProjectTimesheets.AsNoTracking().SingleAsync(timesheet => timesheet.Id == workflow.ProjectTimesheetId);
        Assert.Equal(TestTimesheetStatusIds.Approved, project.TimesheetStatusId);
        Assert.Equal(manager.Id, project.LockedBy);
        Assert.NotNull(project.LockedAt);
    }

    [Fact]
    public async Task Employee_CannotApproveProjectPart()
    {
        WorkflowSetup workflow = await CreateWorkflowSetupAsync(TestTimesheetStatusIds.Submitted, TestTimesheetStatusIds.Draft);
        UpdateCombinedTimesheetStatus.Request request = new(workflow.EmployeeId, workflow.Year, workflow.Month, "approve", null, [workflow.ProjectTimesheetId]);

        HttpStatusCode statusCode = await PutStatusAsAsync(workflow.EmployeePersonalNumber, request);

        Assert.Equal(HttpStatusCode.Unauthorized, statusCode);
    }

    [Fact]
    public async Task FinalApproval_RequiresApprovedProjectParts()
    {
        WorkflowSetup workflow = await CreateWorkflowSetupAsync(TestTimesheetStatusIds.Submitted, TestTimesheetStatusIds.Draft);
        UpdateCombinedTimesheetStatus.Request request = new(workflow.EmployeeId, workflow.Year, workflow.Month, "approve", null, [workflow.AttendanceTimesheetId]);

        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/timesheets/combined/status", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Employee_CanFinalApproveOwnWholeTimesheetAfterProjectPartsAreApproved()
    {
        WorkflowSetup workflow = await CreateWorkflowSetupAsync(TestTimesheetStatusIds.Submitted, TestTimesheetStatusIds.Approved);
        UpdateCombinedTimesheetStatus.Request request = new(workflow.EmployeeId, workflow.Year, workflow.Month, "approve", null, [workflow.AttendanceTimesheetId]);

        HttpStatusCode statusCode = await PutStatusAsAsync(workflow.EmployeePersonalNumber, request);

        Assert.Equal(HttpStatusCode.OK, statusCode);
        using IServiceScope assertionScope = CreateScope();
        AppDbContext assertionContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        AttendanceTimesheet attendance = await assertionContext.AttendanceTimesheets.AsNoTracking().SingleAsync(timesheet => timesheet.Id == workflow.AttendanceTimesheetId);
        Assert.Equal(TestTimesheetStatusIds.Approved, attendance.TimesheetStatusId);
        Assert.Equal(workflow.EmployeeId, attendance.ApprovedBy);
        Assert.NotNull(attendance.ApprovedAt);
    }

    private async Task<WorkflowSetup> CreateWorkflowSetupAsync(Guid attendanceStatusId, Guid projectStatusId)
    {
        const int year = 2048;
        const int month = 1;
        DateTime periodStart = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, periodStart, periodStart.AddMonths(1).AddDays(-1), workload: 0.5m);
        Guid attendanceTimesheetId = Guid.CreateVersion7();

        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        ProjectTimesheet projectTimesheet = await dbContext.ProjectTimesheets.SingleAsync(timesheet => timesheet.ContractEmployeeId == setup.ContractEmployeeId && timesheet.Year == year && timesheet.Month == month);
        projectTimesheet.TimesheetStatusId = projectStatusId;
        projectTimesheet.LockedAt = projectStatusId == TestTimesheetStatusIds.Approved ? DateTime.UtcNow : null;
        projectTimesheet.LockedBy = projectStatusId == TestTimesheetStatusIds.Approved ? setup.EmployeeId : null;

        Guid? employeeTypeId = await dbContext.Employees
            .Where(employee => employee.Id == setup.EmployeeId)
            .Select(employee => employee.EmployeeTypeId)
            .SingleAsync();

        dbContext.AttendanceTimesheets.Add(new AttendanceTimesheet
        {
            Id = attendanceTimesheetId,
            EmployeeId = setup.EmployeeId,
            EmployeeTypeId = employeeTypeId,
            TimesheetStatusId = attendanceStatusId,
            Year = year,
            Month = month,
            CreatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        return new WorkflowSetup(setup.EmployeeId, setup.EmployeePersonalNumber, attendanceTimesheetId, projectTimesheet.Id, setup.ProjectId, year, month);
    }

    private async Task<HttpStatusCode> PutStatusAsAsync(string personalNumber, UpdateCombinedTimesheetStatus.Request request)
    {
        using WebApplicationFactory<Program> factory = CreateAuthenticatedFactory();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using HttpRequestMessage message = new(HttpMethod.Put, "/api/timesheets/combined/status")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add(TestAuthHandler.PersonalNumberHeader, personalNumber);
        using HttpResponseMessage response = await client.SendAsync(message);
        return response.StatusCode;
    }

    private WebApplicationFactory<Program> CreateAuthenticatedFactory() => Factory.WithWebHostBuilder(builder =>
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Enabled"] = "true"
            })));

    private sealed record WorkflowSetup(Guid EmployeeId, string EmployeePersonalNumber, Guid AttendanceTimesheetId, Guid ProjectTimesheetId, Guid ProjectId, int Year, int Month);
}
