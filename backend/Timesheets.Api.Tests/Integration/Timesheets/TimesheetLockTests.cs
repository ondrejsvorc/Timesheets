using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;
using Timesheets.Api.Features.Employees;
using ProjectColumnEdit = Timesheets.Api.Features.Timesheets.ProjectColumnEdit;
using ProjectDayEdit = Timesheets.Api.Features.Timesheets.ProjectDayEdit;
using TimesheetDayEdit = Timesheets.Api.Features.Timesheets.TimesheetDayEdit;
using TimesheetEditRequest = Timesheets.Api.Features.Timesheets.TimesheetEditRequest;

namespace Timesheets.Api.Tests.Integration.Timesheets;

public class TimesheetLockTests : BaseIntegrationTest
{
    public TimesheetLockTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task UpdateTimesheet_LockedHoursAndMetadataCannotChange()
    {
        DateTime date = new(2035, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid contractEmployeeId = Guid.CreateVersion7();
        Guid projectTimesheetId = Guid.CreateVersion7();
        await SeedTimesheetsAsync(attendanceTimesheetId, contractEmployeeId, projectTimesheetId, date);

        TimesheetEditRequest forgedRequest = CreateDraft(contractEmployeeId, date, hours: 9m);
        HttpResponseMessage forgedResponse = await Client.PutAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}", forgedRequest);
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
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid contractEmployeeId = Guid.CreateVersion7();
        Guid projectTimesheetId = Guid.CreateVersion7();
        await SeedTimesheetsAsync(attendanceTimesheetId, contractEmployeeId, projectTimesheetId, date, locked: false);

        TimesheetEditRequest request = CreateDraft(contractEmployeeId, date, hours: 2m, coreHours: 1m);
        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using IServiceScope assertionScope = CreateScope();
        AppDbContext assertionContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        AttendanceDay storedDay = await assertionContext.AttendanceDays.AsNoTracking().SingleAsync(day => day.AttendanceId == attendanceTimesheetId);
        Assert.Equal(1m, storedDay.CoreHours);
    }

    [Fact]
    public async Task UpdateTimesheet_ProjectCellLockIsPersisted()
    {
        DateTime date = new(2035, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid contractEmployeeId = Guid.CreateVersion7();
        Guid projectTimesheetId = Guid.CreateVersion7();
        await SeedTimesheetsAsync(attendanceTimesheetId, contractEmployeeId, projectTimesheetId, date, locked: false);

        TimesheetEditRequest request = CreateDraft(contractEmployeeId, date, hours: 9m, hoursLocked: true);
        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using IServiceScope assertionScope = CreateScope();
        AppDbContext assertionContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        ProjectDay stored = await assertionContext.ProjectDays.AsNoTracking().SingleAsync(day => day.ProjectTimesheetId == projectTimesheetId);
        Assert.Equal(9m, stored.Hours);
        Assert.True(stored.HoursLocked);
    }

    private async Task SeedTimesheetsAsync(Guid attendanceTimesheetId, Guid contractEmployeeId, Guid projectTimesheetId, DateTime date, bool locked = true)
    {
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.ContractEmployees.Add(new ContractEmployee { Id = contractEmployeeId, ContractId = SeededTestData.BetaContractId, EmployeeId = SeededTestData.JanNovakEmployeeId, PositionCode = "LOCK", Position = $"Lock test {contractEmployeeId}", Workload = 1m, StartDate = date, EndDate = date });
        TimesheetBootstrap.AddLegacyMonthWithDays(
            dbContext,
            new AttendanceTimesheet { Id = attendanceTimesheetId, EmployeeId = SeededTestData.JanNovakEmployeeId, EmployeeTypeId = EmployeeTypes.AcademicId, TimesheetStatusId = TestTimesheetStatusIds.Draft, Year = date.Year, Month = date.Month },
            [new AttendanceDay { Id = Guid.CreateVersion7(), Date = date, Workload = 1m, HoursWithoutBreak = 8m, HoursObligation = 8m, CoreHours = 0m, Schedules = "[]" }]);
        dbContext.ProjectTimesheets.Add(new ProjectTimesheet { Id = projectTimesheetId, EmployeeId = SeededTestData.JanNovakEmployeeId, ContractId = SeededTestData.BetaContractId, ContractEmployeeId = contractEmployeeId, TimesheetStatusId = locked ? TestTimesheetStatusIds.Approved : TestTimesheetStatusIds.Draft, Year = date.Year, Month = date.Month, Workload = 1m, LockedAt = locked ? DateTime.UtcNow : null, LockedBy = locked ? SeededTestData.JanNovakEmployeeId : null, Days = [new ProjectDay { Id = Guid.CreateVersion7(), Date = date, Hours = 2m, Workload = 1m, HoursObligation = 8m }] });

        await dbContext.SaveChangesAsync();
    }

    private static TimesheetEditRequest CreateDraft(Guid contractEmployeeId, DateTime date, decimal hours, decimal coreHours = 0m, bool hoursLocked = false)
    {
        TimesheetDayEdit attendanceDay = new(Date: date, ClockIn: null, ClockOut: null, BreakStart: null, BreakEnd: null, CoreHours: coreHours, Description: null, Schedules: []);
        ProjectColumnEdit project = new(ContractEmployeeId: contractEmployeeId, Days: [new ProjectDayEdit(Date: date, Hours: hours, HoursLocked: hoursLocked)]);
        return new TimesheetEditRequest(Days: [attendanceDay], Projects: [project]);
    }
}
