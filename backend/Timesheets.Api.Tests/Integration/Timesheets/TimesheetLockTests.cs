using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Domain;
using Timesheets.Api.Domain.Models;
using Timesheets.Api.Features.Employees;
using ContractPartDayEdit = Timesheets.Api.Features.Timesheets.ContractPartDayEdit;
using ContractPartEdit = Timesheets.Api.Features.Timesheets.ContractPartEdit;
using DayEdit = Timesheets.Api.Features.Timesheets.DayEdit;
using TimesheetEdit = Timesheets.Api.Features.Timesheets.TimesheetEdit;

namespace Timesheets.Api.Tests.Integration.Timesheets;

public class TimesheetLockTests : BaseIntegrationTest
{
    public TimesheetLockTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task UpdateTimesheet_LockedHoursAndMetadataCannotChange()
    {
        DateTime date = new(2035, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        Guid timesheetId = Guid.CreateVersion7();
        Guid contractEmployeeId = Guid.CreateVersion7();
        Guid contractPartId = Guid.CreateVersion7();
        await SeedTimesheetsAsync(timesheetId, contractEmployeeId, contractPartId, date);

        TimesheetEdit forgedRequest = CreateDraft(contractEmployeeId, date, hours: 9m);
        HttpResponseMessage forgedResponse = await Client.PutAsJsonAsync($"/api/timesheets/{timesheetId}", forgedRequest);
        Assert.Equal(HttpStatusCode.OK, forgedResponse.StatusCode);

        using IServiceScope assertionScope = CreateScope();
        AppDbContext assertionContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        ContractPart stored = await assertionContext.ContractParts.AsNoTracking().Include(timesheet => timesheet.Days).SingleAsync(timesheet => timesheet.Id == contractPartId);
        Assert.NotNull(stored.LockedAt);
        Assert.Equal(SeededTestData.JanNovakEmployeeId, stored.LockedBy);
        Assert.Equal(2m, Assert.Single(stored.Days).Hours);
    }

    [Fact]
    public async Task UpdateTimesheet_CoreHoursCanChangeBeforeProjectsAreLocked()
    {
        DateTime date = new(2035, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        Guid timesheetId = Guid.CreateVersion7();
        Guid contractEmployeeId = Guid.CreateVersion7();
        Guid contractPartId = Guid.CreateVersion7();
        await SeedTimesheetsAsync(timesheetId, contractEmployeeId, contractPartId, date, locked: false);

        TimesheetEdit request = CreateDraft(contractEmployeeId, date, hours: 2m, coreHours: 1m);
        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/timesheets/{timesheetId}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using IServiceScope assertionScope = CreateScope();
        AppDbContext assertionContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        AttendanceDay storedDay = await assertionContext.AttendanceDays.AsNoTracking().SingleAsync(day => day.AttendanceId == timesheetId);
        Assert.Equal(1m, storedDay.CoreHours);
    }

    [Fact]
    public async Task UpdateTimesheet_ContractPartCellLockIsPersisted()
    {
        DateTime date = new(2035, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        Guid timesheetId = Guid.CreateVersion7();
        Guid contractEmployeeId = Guid.CreateVersion7();
        Guid contractPartId = Guid.CreateVersion7();
        await SeedTimesheetsAsync(timesheetId, contractEmployeeId, contractPartId, date, locked: false);

        TimesheetEdit request = CreateDraft(contractEmployeeId, date, hours: 9m, hoursLocked: true);
        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/timesheets/{timesheetId}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using IServiceScope assertionScope = CreateScope();
        AppDbContext assertionContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        ContractPartDay stored = await assertionContext.ContractPartDays.AsNoTracking().SingleAsync(day => day.ContractPartId == contractPartId);
        Assert.Equal(9m, stored.Hours);
        Assert.True(stored.HoursLocked);
    }

    [Fact]
    public async Task UpdateTimesheet_HalfDayInterruptionDoesNotPersistContractPartCellLock()
    {
        DateTime date = new(2035, 4, 2, 0, 0, 0, DateTimeKind.Utc);
        Guid timesheetId = Guid.CreateVersion7();
        Guid contractEmployeeId = Guid.CreateVersion7();
        Guid contractPartId = Guid.CreateVersion7();
        await SeedTimesheetsAsync(timesheetId, contractEmployeeId, contractPartId, date, locked: false);

        TimesheetEdit request = CreateDraft(contractEmployeeId, date, hours: 4m, hoursLocked: true, description: "ZV p\u016flden");
        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/timesheets/{timesheetId}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using IServiceScope assertionScope = CreateScope();
        AppDbContext assertionContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        ContractPartDay stored = await assertionContext.ContractPartDays.AsNoTracking().SingleAsync(day => day.ContractPartId == contractPartId);
        Assert.Equal(4m, stored.Hours);
        Assert.False(stored.HoursLocked);
    }

    private async Task SeedTimesheetsAsync(Guid timesheetId, Guid contractEmployeeId, Guid contractPartId, DateTime date, bool locked = true)
    {
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.ContractEmployees.Add(new ContractEmployee { Id = contractEmployeeId, ContractId = SeededTestData.BetaContractId, EmployeeId = SeededTestData.JanNovakEmployeeId, PositionCode = "LOCK", Position = $"Lock test {contractEmployeeId}", Workload = 1m, StartDate = date, EndDate = date });
        TimesheetBootstrap.AddMonthWithDays(
            dbContext,
            new Timesheet { Id = timesheetId, EmployeeId = SeededTestData.JanNovakEmployeeId, TimesheetStatusId = TestTimesheetStatusIds.Draft, Year = date.Year, Month = date.Month },
            EmployeeTypes.AcademicId,
            [new AttendanceDay { Id = Guid.CreateVersion7(), Date = date, Workload = 1m, HoursWithoutBreak = 8m, HoursObligation = 8m, CoreHours = 0m, Schedules = "[]" }]);
        dbContext.ContractParts.Add(new ContractPart { Id = contractPartId, TimesheetId = timesheetId, ContractEmployeeId = contractEmployeeId, TimesheetStatusId = locked ? TestTimesheetStatusIds.Approved : TestTimesheetStatusIds.Draft, Workload = 1m, LockedAt = locked ? DateTime.UtcNow : null, LockedBy = locked ? SeededTestData.JanNovakEmployeeId : null, Days = [new ContractPartDay { Id = Guid.CreateVersion7(), Date = date, Hours = 2m, HoursObligation = 8m }] });

        await dbContext.SaveChangesAsync();
    }

    private static TimesheetEdit CreateDraft(Guid contractEmployeeId, DateTime date, decimal hours, decimal coreHours = 0m, bool hoursLocked = false, string? description = null)
    {
        DayEdit attendanceDay = new(Date: date, ClockIn: null, ClockOut: null, BreakStart: null, BreakEnd: null, CoreHours: coreHours, Description: description, Schedules: []);
        ContractPartEdit project = new(ContractEmployeeId: contractEmployeeId, Days: [new ContractPartDayEdit(Date: date, Hours: hours, HoursLocked: hoursLocked)]);
        return new TimesheetEdit(Days: [attendanceDay], ContractParts: [project]);
    }
}
