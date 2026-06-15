using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;
using TimesheetDraft = Timesheets.Api.Timesheets.TimesheetDraft;
using TimesheetDraftDay = Timesheets.Api.Timesheets.TimesheetDraftDay;
using TimesheetDraftProject = Timesheets.Api.Timesheets.TimesheetDraftProject;
using TimesheetDraftProjectDay = Timesheets.Api.Timesheets.TimesheetDraftProjectDay;

namespace Timesheets.Api.Tests.Integration.Timesheets;

public class TimesheetLockTests : BaseIntegrationTest
{
    public TimesheetLockTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task UpdateTimesheet_LockedHoursAndMetadataCannotChange()
    {
        DateTime date = new(2035, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        Guid attendanceTimesheetId = Guid.NewGuid();
        Guid contractEmployeeId = Guid.NewGuid();
        Guid projectTimesheetId = Guid.NewGuid();
        await SeedTimesheetsAsync(attendanceTimesheetId, contractEmployeeId, projectTimesheetId, date);

        TimesheetDraft forgedDraft = CreateDraft(contractEmployeeId, date, hours: 9m);
        HttpResponseMessage forgedResponse = await Client.PutAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}", forgedDraft);
        Assert.Equal(HttpStatusCode.OK, forgedResponse.StatusCode);

        using IServiceScope assertionScope = CreateScope();
        AppDbContext assertionContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        ProjectTimesheet stored = await assertionContext.ProjectTimesheets.AsNoTracking().Include(timesheet => timesheet.Days).SingleAsync(timesheet => timesheet.Id == projectTimesheetId);
        Assert.NotNull(stored.LockedAt);
        Assert.Equal(SeededTestData.JanNovakEmployeeId, stored.LockedBy);
        Assert.Equal(2m, Assert.Single(stored.Days).Hours);
    }

    [Fact]
    public async Task UpdateTimesheet_CoreHoursCanChangeBeforeProjectsAreLocked()
    {
        DateTime date = new(2035, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        Guid attendanceTimesheetId = Guid.NewGuid();
        Guid contractEmployeeId = Guid.NewGuid();
        Guid projectTimesheetId = Guid.NewGuid();
        await SeedTimesheetsAsync(attendanceTimesheetId, contractEmployeeId, projectTimesheetId, date, locked: false);

        TimesheetDraft draft = CreateDraft(contractEmployeeId, date, hours: 2m, coreHours: 1m);
        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}", draft);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using IServiceScope assertionScope = CreateScope();
        AppDbContext assertionContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        AttendanceTimesheet stored = await assertionContext.AttendanceTimesheets.AsNoTracking().Include(timesheet => timesheet.Days).SingleAsync(timesheet => timesheet.Id == attendanceTimesheetId);
        Assert.Equal(1m, Assert.Single(stored.Days).CoreHours);
    }

    private async Task SeedTimesheetsAsync(Guid attendanceTimesheetId, Guid contractEmployeeId, Guid projectTimesheetId, DateTime date, bool locked = true)
    {
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.ContractEmployees.Add(new ContractEmployee { Id = contractEmployeeId, ContractId = SeededTestData.AlphaContractId, EmployeeId = SeededTestData.JanNovakEmployeeId, PositionCode = "LOCK", Position = $"Lock test {contractEmployeeId}", Workload = 1m, StartDate = date, EndDate = date });
        dbContext.AttendanceTimesheets.Add(new AttendanceTimesheet { Id = attendanceTimesheetId, EmployeeId = SeededTestData.JanNovakEmployeeId, TimesheetStatusId = TestTimesheetStatusIds.Draft, Year = date.Year, Month = date.Month, Days = [new AttendanceDay { Id = Guid.NewGuid(), Date = date, Workload = 1m, HoursWithoutBreak = 8m, HoursObligation = 8m, CoreHours = 0m, Schedules = "[]" }] });
        dbContext.ProjectTimesheets.Add(new ProjectTimesheet { Id = projectTimesheetId, EmployeeId = SeededTestData.JanNovakEmployeeId, ContractId = SeededTestData.AlphaContractId, ContractEmployeeId = contractEmployeeId, TimesheetStatusId = locked ? TestTimesheetStatusIds.Approved : TestTimesheetStatusIds.Draft, Year = date.Year, Month = date.Month, Workload = 1m, LockedAt = locked ? DateTime.UtcNow : null, LockedBy = locked ? SeededTestData.JanNovakEmployeeId : null, Days = [new ProjectDay { Id = Guid.NewGuid(), Date = date, Hours = 2m, Workload = 1m, HoursObligation = 8m }] });

        await dbContext.SaveChangesAsync();
    }

    private static TimesheetDraft CreateDraft(Guid contractEmployeeId, DateTime date, decimal hours, decimal coreHours = 0m)
    {
        TimesheetDraftDay attendanceDay = new(Date: date, ClockIn: null, ClockOut: null, BreakStart: null, BreakEnd: null, CoreHours: coreHours, Description: null, Schedules: []);
        TimesheetDraftProject project = new(ContractEmployeeId: contractEmployeeId, Days: [new TimesheetDraftProjectDay(Date: date, Hours: hours)]);
        return new TimesheetDraft(Days: [attendanceDay], Projects: [project]);
    }
}
