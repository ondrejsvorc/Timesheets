using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;
using Timesheets.Api.Timesheets;

namespace Timesheets.Api.Tests.Integration.Timesheets;

public sealed class TimesheetInterruptionAllocationTests : BaseIntegrationTest
{
    private static readonly Guid AcademicEmployeeTypeId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid NonAcademicEmployeeTypeId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public TimesheetInterruptionAllocationTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task AllocateTimesheet_UsesExplicitInterruptionRules()
    {
        DateTime firstDate = new(2036, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        Guid attendanceTimesheetId = Guid.NewGuid();
        Guid firstAssignmentId = Guid.NewGuid();
        Guid secondAssignmentId = Guid.NewGuid();
        await SeedAsync(attendanceTimesheetId, firstAssignmentId, secondAssignmentId, firstDate);

        TimesheetDraft draft = new(
            Days:
            [
                Day(firstDate, "NK"),
                Day(firstDate.AddDays(1), "D"),
                Day(firstDate.AddDays(2), "SCT")
            ],
            Projects:
            [
                Project(firstAssignmentId, firstDate),
                Project(secondAssignmentId, firstDate)
            ]);

        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate", draft);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        TimesheetAllocation? allocation = await response.Content.ReadFromJsonAsync<TimesheetAllocation>();
        Assert.NotNull(allocation);

        TimesheetAllocationDay doctor = allocation!.Days.Single(day => day.Date == firstDate);
        Assert.Equal(8m, doctor.CoreHours);
        Assert.All(doctor.ProjectHours.Values, hours => Assert.Equal(0m, hours));

        TimesheetAllocationDay proportional = allocation.Days.Single(day => day.Date == firstDate.AddDays(1));
        Assert.Equal(4m, proportional.CoreHours);
        Assert.Equal(2m, proportional.ProjectHours[firstAssignmentId]);
        Assert.Equal(2m, proportional.ProjectHours[secondAssignmentId]);

        TimesheetAllocationDay businessTrip = allocation.Days.Single(day => day.Date == firstDate.AddDays(2));
        Assert.Equal(0m, businessTrip.CoreHours);
        Assert.All(businessTrip.ProjectHours.Values, hours => Assert.Equal(0m, hours));
    }

    [Fact]
    public async Task AllocateTimesheet_TopsUpPartialCoreToStagMinimum()
    {
        DateTime date = new(2036, 2, 4, 0, 0, 0, DateTimeKind.Utc);
        Guid attendanceTimesheetId = Guid.NewGuid();
        await SeedSingleDayAsync(attendanceTimesheetId, date, AcademicEmployeeTypeId, assignmentId: null);

        TimesheetDraft draft = new(
            Days:
            [
                new TimesheetDraftDay(
                    Date: date,
                    ClockIn: null,
                    ClockOut: null,
                    BreakStart: null,
                    BreakEnd: null,
                    CoreHours: 1m,
                    Description: null,
                    Schedules: [new TimeRange(new TimeSpan(8, 0, 0), new TimeSpan(9, 50, 0))])
            ],
            Projects: []);

        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate", draft);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        TimesheetAllocation? allocation = await response.Content.ReadFromJsonAsync<TimesheetAllocation>();
        Assert.NotNull(allocation);
        Assert.Equal(2m, allocation!.Days.Single().CoreHours);
    }

    [Fact]
    public async Task AllocateTimesheet_DoesNotFillNonAcademicDayWithoutAttendance()
    {
        DateTime date = new(2036, 3, 3, 0, 0, 0, DateTimeKind.Utc);
        Guid attendanceTimesheetId = Guid.NewGuid();
        Guid assignmentId = Guid.NewGuid();
        await SeedSingleDayAsync(attendanceTimesheetId, date, NonAcademicEmployeeTypeId, assignmentId);

        TimesheetDraft draft = new(
            Days: [new TimesheetDraftDay(Date: date, ClockIn: null, ClockOut: null, BreakStart: null, BreakEnd: null, CoreHours: 0m, Description: null, Schedules: [])],
            Projects: [new TimesheetDraftProject(assignmentId, [new TimesheetDraftProjectDay(date, 0m)])]);

        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate", draft);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        TimesheetAllocation? allocation = await response.Content.ReadFromJsonAsync<TimesheetAllocation>();
        Assert.NotNull(allocation);
        TimesheetAllocationDay day = allocation!.Days.Single();
        Assert.Equal(0m, day.CoreHours);
        Assert.Equal(0m, day.ProjectHours[assignmentId]);
    }

    [Fact]
    public async Task AllocateTimesheet_PrefersQuarterProjectsAndLeavesRemainderInCore()
    {
        DateTime date = new(2036, 4, 2, 0, 0, 0, DateTimeKind.Utc);
        Guid attendanceTimesheetId = Guid.NewGuid();
        Guid assignmentId = Guid.NewGuid();
        await SeedSingleDayAsync(attendanceTimesheetId, date, AcademicEmployeeTypeId, assignmentId, assignmentWorkload: 0.3m);

        TimesheetDraft draft = new(
            Days: [new TimesheetDraftDay(Date: date, ClockIn: null, ClockOut: null, BreakStart: null, BreakEnd: null, CoreHours: 0m, Description: null, Schedules: [])],
            Projects: [new TimesheetDraftProject(assignmentId, [new TimesheetDraftProjectDay(date, 0m)])]);

        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate", draft);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        TimesheetAllocation? allocation = await response.Content.ReadFromJsonAsync<TimesheetAllocation>();
        Assert.NotNull(allocation);
        TimesheetAllocationDay day = allocation!.Days.Single();
        Assert.Equal(5.75m, day.CoreHours);
        Assert.Equal(2.25m, day.ProjectHours[assignmentId]);
    }

    private async Task SeedAsync(Guid attendanceTimesheetId, Guid firstAssignmentId, Guid secondAssignmentId, DateTime firstDate)
    {
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        ContractEmployee firstAssignment = Assignment(firstAssignmentId, "INT-1", "Interruption 1", firstDate);
        ContractEmployee secondAssignment = Assignment(secondAssignmentId, "INT-2", "Interruption 2", firstDate);
        dbContext.ContractEmployees.AddRange(firstAssignment, secondAssignment);
        dbContext.EmployeeWorkloads.Add(new EmployeeWorkload { Id = Guid.NewGuid(), EmployeeId = SeededTestData.JanNovakEmployeeId, Year = firstDate.Year, Month = firstDate.Month, Workload = 1m });
        dbContext.AttendanceTimesheets.Add(new Data.Models.AttendanceTimesheet
        {
            Id = attendanceTimesheetId,
            EmployeeId = SeededTestData.JanNovakEmployeeId,
            TimesheetStatusId = TestTimesheetStatusIds.Draft,
            Year = firstDate.Year,
            Month = firstDate.Month,
            Days =
            [
                AttendanceDay(firstDate, "NK"),
                AttendanceDay(firstDate.AddDays(1), "D"),
                AttendanceDay(firstDate.AddDays(2), "SCT")
            ]
        });
        dbContext.ProjectTimesheets.AddRange(ProjectTimesheet(firstAssignmentId, firstDate), ProjectTimesheet(secondAssignmentId, firstDate));
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedSingleDayAsync(Guid attendanceTimesheetId, DateTime date, Guid employeeTypeId, Guid? assignmentId, decimal assignmentWorkload = 0.5m)
    {
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Employee employee = await dbContext.Employees.SingleAsync(employee => employee.Id == SeededTestData.JanNovakEmployeeId);
        employee.EmployeeTypeId = employeeTypeId;
        dbContext.EmployeeWorkloads.Add(new EmployeeWorkload { Id = Guid.NewGuid(), EmployeeId = SeededTestData.JanNovakEmployeeId, Year = date.Year, Month = date.Month, Workload = 1m });
        dbContext.AttendanceTimesheets.Add(new Data.Models.AttendanceTimesheet
        {
            Id = attendanceTimesheetId,
            EmployeeId = SeededTestData.JanNovakEmployeeId,
            TimesheetStatusId = TestTimesheetStatusIds.Draft,
            Year = date.Year,
            Month = date.Month,
            Days = [AttendanceDay(date, null)]
        });

        if (assignmentId.HasValue)
        {
            dbContext.ContractEmployees.Add(Assignment(assignmentId.Value, $"GEN-{date:yyyy-MM}", $"Generation {date:yyyy-MM}", date, assignmentWorkload));
            dbContext.ProjectTimesheets.Add(new Data.Models.ProjectTimesheet
            {
                Id = Guid.NewGuid(),
                EmployeeId = SeededTestData.JanNovakEmployeeId,
                ContractId = SeededTestData.AlphaContractId,
                ContractEmployeeId = assignmentId.Value,
                TimesheetStatusId = TestTimesheetStatusIds.Draft,
                Year = date.Year,
                Month = date.Month,
                Workload = assignmentWorkload,
                Days = [ProjectDay(date, assignmentWorkload)]
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static ContractEmployee Assignment(Guid id, string code, string position, DateTime date, decimal workload = 0.25m) => new()
    {
        Id = id,
        ContractId = SeededTestData.AlphaContractId,
        EmployeeId = SeededTestData.JanNovakEmployeeId,
        PositionCode = code,
        Position = position,
        Workload = workload,
        StartDate = date,
        EndDate = date.AddDays(2)
    };

    private static Data.Models.AttendanceDay AttendanceDay(DateTime date, string? description) => new() { Id = Guid.NewGuid(), Date = date, Workload = 1m, HoursObligation = 8m, Description = description, Schedules = "[]" };

    private static Data.Models.ProjectTimesheet ProjectTimesheet(Guid assignmentId, DateTime firstDate) => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = SeededTestData.JanNovakEmployeeId,
        ContractId = SeededTestData.AlphaContractId,
        ContractEmployeeId = assignmentId,
        TimesheetStatusId = TestTimesheetStatusIds.Draft,
        Year = firstDate.Year,
        Month = firstDate.Month,
        Workload = 0.25m,
        Days =
        [
            ProjectDay(firstDate),
            ProjectDay(firstDate.AddDays(1)),
            ProjectDay(firstDate.AddDays(2))
        ]
    };

    private static Data.Models.ProjectDay ProjectDay(DateTime date, decimal workload = 0.25m) => new() { Id = Guid.NewGuid(), Date = date, Workload = workload, HoursObligation = 8m * workload };
    private static TimesheetDraftDay Day(DateTime date, string description) => new(Date: date, ClockIn: null, ClockOut: null, BreakStart: null, BreakEnd: null, CoreHours: 0m, Description: description, Schedules: []);
    private static TimesheetDraftProject Project(Guid assignmentId, DateTime firstDate) => new(ContractEmployeeId: assignmentId, Days: [new TimesheetDraftProjectDay(firstDate, 0m), new TimesheetDraftProjectDay(firstDate.AddDays(1), 0m), new TimesheetDraftProjectDay(firstDate.AddDays(2), 0m)]);
}
