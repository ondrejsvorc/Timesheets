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
    public async Task UnsupportedAction_ReturnsBadRequest()
    {
        UpdateCombinedTimesheetStatus.Request request = new(SeededTestData.PetrEmployeeId, 2024, 12, "delete", null, [SeededTestData.PetrDecProjectTimesheetId]);

        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/timesheets/combined/status", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GlobalManager_CannotChangeAnotherEmployeesWholeStatus()
    {
        UpdateCombinedTimesheetStatus.Request request = new(SeededTestData.MarieEmployeeId, 2024, 12, "approve", null, [Guid.Parse("70000000-0000-0000-0000-000000000002")]);

        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/timesheets/combined/status", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
