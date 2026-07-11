using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;
using Timesheets.Api.Features.Employees;
using Timesheets.Api.Features.Timesheets;
using Timesheets.Api.Features.Timesheets.Endpoints;

namespace Timesheets.Api.Tests.Integration.Timesheets;

public sealed class TimesheetEmployeeTypeSnapshotTests : BaseIntegrationTest
{
    private static readonly Guid NonAcademicEmployeeTypeId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public TimesheetEmployeeTypeSnapshotTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetCombinedTimesheet_UsesEmployeeTypeSnapshot()
    {
        SnapshotSetup setup = await SeedSnapshotTimesheetAsync(year: 2058, month: 1, snapshotEmployeeTypeId: EmployeeTypes.AcademicId, currentEmployeeTypeId: NonAcademicEmployeeTypeId);

        HttpResponseMessage response = await Client.GetAsync($"/api/timesheets/combined?employeeId={setup.EmployeeId}&year={setup.Year}&month={setup.Month}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        GetCombinedTimesheet.Response? payload = await response.Content.ReadFromJsonAsync<GetCombinedTimesheet.Response>();
        Assert.NotNull(payload);
        Assert.False(payload!.TracksAttendance);
    }

    [Fact]
    public async Task ReviewTimesheet_UsesEmployeeTypeSnapshot()
    {
        SnapshotSetup setup = await SeedSnapshotTimesheetAsync(year: 2059, month: 1, snapshotEmployeeTypeId: EmployeeTypes.AcademicId, currentEmployeeTypeId: NonAcademicEmployeeTypeId);
        DateTime date = new(setup.Year, setup.Month, 2, 0, 0, 0, DateTimeKind.Utc);
        TimesheetEditRequest request = new(
            Days:
            [
                new TimesheetDayEdit(
                    Date: date,
                    ClockIn: null,
                    ClockOut: null,
                    BreakStart: null,
                    BreakEnd: null,
                    CoreHours: 1m,
                    Description: null,
                    Schedules: [new TimeRange(new TimeSpan(8, 0, 0), new TimeSpan(10, 0, 0))])
            ],
            Projects: []);

        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{setup.AttendanceTimesheetId}/review", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        TimesheetEvaluation? evaluation = await response.Content.ReadFromJsonAsync<TimesheetEvaluation>();
        Assert.NotNull(evaluation);
        Assert.Contains(evaluation!.DayIssues, issue => issue.Code == "ERR-ALL-02");
    }

    [Fact]
    public async Task AllocateTimesheet_UsesEmployeeTypeSnapshot()
    {
        SnapshotSetup setup = await SeedSnapshotTimesheetAsync(year: 2060, month: 1, snapshotEmployeeTypeId: EmployeeTypes.AcademicId, currentEmployeeTypeId: NonAcademicEmployeeTypeId);
        DateTime date = new(setup.Year, setup.Month, 2, 0, 0, 0, DateTimeKind.Utc);
        TimesheetEditRequest request = new(
            Days:
            [
                new TimesheetDayEdit(
                    Date: date,
                    ClockIn: null,
                    ClockOut: null,
                    BreakStart: null,
                    BreakEnd: null,
                    CoreHours: 0m,
                    Description: null,
                    Schedules: [new TimeRange(new TimeSpan(8, 0, 0), new TimeSpan(10, 0, 0))])
            ],
            Projects: []);

        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{setup.AttendanceTimesheetId}/allocate?day=2", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
        Assert.NotNull(allocation);
        AllocateTimesheet.DayResponse day = allocation!.Days.Single();
        Assert.True(day.CoreHours >= 2m);
        Assert.Null(day.Work[0]);
        Assert.Null(day.Work[1]);
    }

    private async Task<SnapshotSetup> SeedSnapshotTimesheetAsync(int year, int month, Guid snapshotEmployeeTypeId, Guid currentEmployeeTypeId)
    {
        string personalNumber = "snap-" + TestIdentifiers.Suffix(16);
        Employee employee = await TestEmployeeFactory.CreateAsync(Factory.Services, personalNumber, "Snapshot", "Employee", snapshotEmployeeTypeId);
        Guid timesheetId = Guid.CreateVersion7();
        DateTime date = new(year, month, 2, 0, 0, 0, DateTimeKind.Utc);

        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Employee storedEmployee = await dbContext.Employees.SingleAsync(item => item.Id == employee.Id);
        storedEmployee.EmployeeTypeId = currentEmployeeTypeId;
        dbContext.EmployeeWorkloads.Add(new EmployeeWorkload { Id = Guid.CreateVersion7(), EmployeeId = employee.Id, Year = year, Month = month, Workload = 1m });
        dbContext.AttendanceTimesheets.Add(new Data.Models.AttendanceTimesheet
        {
            Id = timesheetId,
            EmployeeId = employee.Id,
            EmployeeTypeId = snapshotEmployeeTypeId,
            TimesheetStatusId = TestTimesheetStatusIds.Draft,
            Year = year,
            Month = month,
            Days =
            [
                new Data.Models.AttendanceDay
                {
                    Id = Guid.CreateVersion7(),
                    Date = date,
                    Workload = 1m,
                    HoursObligation = 8m,
                    Schedules = "[]"
                }
            ]
        });
        await dbContext.SaveChangesAsync();

        return new SnapshotSetup(employee.Id, timesheetId, year, month);
    }

    private sealed record SnapshotSetup(Guid EmployeeId, Guid AttendanceTimesheetId, int Year, int Month);
}
