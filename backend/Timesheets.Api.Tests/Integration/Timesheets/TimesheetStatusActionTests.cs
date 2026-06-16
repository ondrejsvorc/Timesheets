using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
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
        Guid attendanceTimesheetId = Guid.NewGuid();
        Guid contractEmployeeId = Guid.NewGuid();
        Guid projectTimesheetId = Guid.NewGuid();

        using (IServiceScope scope = CreateScope())
        {
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            DateTime periodStart = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            AttendanceDay[] days = Enumerable.Range(1, DateTime.DaysInMonth(year, month))
                .Select(day => new AttendanceDay { Id = Guid.NewGuid(), Date = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc), Workload = 0m, HoursObligation = 0m, Schedules = "[]" })
                .ToArray();

            dbContext.ContractManagers.Add(new ContractManager { Id = Guid.NewGuid(), ContractId = SeededTestData.AlphaContractId, EmployeeId = SeededTestData.MarieEmployeeId });
            dbContext.ContractEmployees.Add(new ContractEmployee { Id = contractEmployeeId, ContractId = SeededTestData.AlphaContractId, EmployeeId = SeededTestData.JanNovakEmployeeId, PositionCode = "WF-2039", Position = "Workflow 2039", Workload = 0m, StartDate = periodStart, EndDate = periodStart.AddMonths(1).AddDays(-1) });
            dbContext.EmployeeWorkloads.Add(new EmployeeWorkload { Id = Guid.NewGuid(), EmployeeId = SeededTestData.JanNovakEmployeeId, Year = year, Month = month, Workload = 0m });
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
    public async Task UnsupportedAction_ReturnsBadRequest()
    {
        UpdateCombinedTimesheetStatus.Request request = new(SeededTestData.PetrEmployeeId, 2024, 12, "delete", null, [SeededTestData.PetrDecProjectTimesheetId]);

        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/timesheets/combined/status", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GlobalManager_CanChangeAnotherEmployeesWholeStatus()
    {
        UpdateCombinedTimesheetStatus.Request request = new(SeededTestData.MarieEmployeeId, 2024, 12, "approve", null, [Guid.Parse("70000000-0000-0000-0000-000000000002")]);

        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/timesheets/combined/status", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Employee_CanCompleteOwnWholeTimesheet()
    {
        Guid attendanceTimesheetId = Guid.NewGuid();
        using (IServiceScope scope = CreateScope())
        {
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.AttendanceTimesheets.Add(new AttendanceTimesheet { Id = attendanceTimesheetId, EmployeeId = SeededTestData.JanNovakEmployeeId, TimesheetStatusId = TestTimesheetStatusIds.Submitted, Year = 2038, Month = 1 });
            await dbContext.SaveChangesAsync();
        }

        UpdateCombinedTimesheetStatus.Request request = new(SeededTestData.JanNovakEmployeeId, 2038, 1, "approve", null, [attendanceTimesheetId]);
        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/timesheets/combined/status", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using IServiceScope assertionScope = CreateScope();
        AppDbContext assertionContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Guid statusId = await assertionContext.AttendanceTimesheets.Where(timesheet => timesheet.Id == attendanceTimesheetId).Select(timesheet => timesheet.TimesheetStatusId).SingleAsync();
        Assert.Equal(TestTimesheetStatusIds.Approved, statusId);
    }
}
