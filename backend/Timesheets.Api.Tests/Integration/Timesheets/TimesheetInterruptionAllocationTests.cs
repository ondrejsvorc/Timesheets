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

    private static ContractEmployee Assignment(Guid id, string code, string position, DateTime date) => new()
    {
        Id = id,
        ContractId = SeededTestData.AlphaContractId,
        EmployeeId = SeededTestData.JanNovakEmployeeId,
        PositionCode = code,
        Position = position,
        Workload = 0.25m,
        StartDate = date,
        EndDate = date.AddDays(2)
    };

    private static Data.Models.AttendanceDay AttendanceDay(DateTime date, string description) => new() { Id = Guid.NewGuid(), Date = date, Workload = 1m, HoursObligation = 8m, Description = description, Schedules = "[]" };

    private static Data.Models.ProjectTimesheet ProjectTimesheet(Guid assignmentId, DateTime firstDate) => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = SeededTestData.JanNovakEmployeeId,
        ContractId = SeededTestData.AlphaContractId,
        ContractEmployeeId = assignmentId,
        TimesheetStatusId = TestTimesheetStatusIds.Approved,
        Year = firstDate.Year,
        Month = firstDate.Month,
        Workload = 0.25m,
        LockedAt = DateTime.UtcNow,
        LockedBy = SeededTestData.JanNovakEmployeeId,
        Days =
        [
            ProjectDay(firstDate),
            ProjectDay(firstDate.AddDays(1)),
            ProjectDay(firstDate.AddDays(2))
        ]
    };

    private static Data.Models.ProjectDay ProjectDay(DateTime date) => new() { Id = Guid.NewGuid(), Date = date, Workload = 0.25m, HoursObligation = 2m };
    private static TimesheetDraftDay Day(DateTime date, string description) => new(Date: date, ClockIn: null, ClockOut: null, BreakStart: null, BreakEnd: null, CoreHours: 0m, Description: description, Schedules: []);
    private static TimesheetDraftProject Project(Guid assignmentId, DateTime firstDate) => new(ContractEmployeeId: assignmentId, Days: [new TimesheetDraftProjectDay(firstDate, 0m), new TimesheetDraftProjectDay(firstDate.AddDays(1), 0m), new TimesheetDraftProjectDay(firstDate.AddDays(2), 0m)]);
}
