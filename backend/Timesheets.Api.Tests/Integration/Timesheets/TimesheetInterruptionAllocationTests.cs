using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;
using Timesheets.Api.Features.Timesheets;
using Timesheets.Api.Features.Timesheets.Endpoints;

namespace Timesheets.Api.Tests.Integration.Timesheets;

public sealed class TimesheetInterruptionAllocationTests : BaseIntegrationTest
{
    private static readonly Guid AcademicEmployeeTypeId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid NonAcademicEmployeeTypeId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public TimesheetInterruptionAllocationTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task AllocateTimesheet_UsesInterruptionRulesFromImis()
    {
        DateTime firstDate = new(2036, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid firstAssignmentId = Guid.CreateVersion7();
        Guid secondAssignmentId = Guid.CreateVersion7();
        await SeedAsync(attendanceTimesheetId, firstAssignmentId, secondAssignmentId, firstDate);

        TimesheetEditRequest request = new(
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

        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
        Assert.NotNull(allocation);

        AllocateTimesheet.DayResponse doctor = allocation!.Days.Single(day => day.Date == firstDate);
        Assert.Equal(4m, doctor.CoreHours);
        Assert.Equal(2m, doctor.ProjectCells[firstAssignmentId].Hours);
        Assert.Equal(2m, doctor.ProjectCells[secondAssignmentId].Hours);

        AllocateTimesheet.DayResponse proportional = allocation.Days.Single(day => day.Date == firstDate.AddDays(1));
        Assert.Equal(4m, proportional.CoreHours);
        Assert.Equal(2m, proportional.ProjectCells[firstAssignmentId].Hours);
        Assert.Equal(2m, proportional.ProjectCells[secondAssignmentId].Hours);

        AllocateTimesheet.DayResponse businessTrip = allocation.Days.Single(day => day.Date == firstDate.AddDays(2));
        Assert.Equal(0m, businessTrip.CoreHours);
        Assert.All(businessTrip.ProjectCells.Values, cell => Assert.Equal(0m, cell.Hours));
    }

    [Fact]
    public async Task AllocateTimesheet_KeepsLockedProjectCellDuringProportionalInterruption()
    {
        DateTime date = new(2036, 8, 2, 0, 0, 0, DateTimeKind.Utc);
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid firstAssignmentId = Guid.CreateVersion7();
        Guid secondAssignmentId = Guid.CreateVersion7();
        await SeedAsync(attendanceTimesheetId, firstAssignmentId, secondAssignmentId, date);

        TimesheetEditRequest request = new(
            Days: [Day(date, "D")],
            Projects:
            [
                new ProjectColumnEdit(firstAssignmentId, [new ProjectDayEdit(date, 5m, HoursLocked: true)]),
                new ProjectColumnEdit(secondAssignmentId, [new ProjectDayEdit(date, 0m)])
            ]);

        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate?day=2", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
        Assert.NotNull(allocation);
        AllocateTimesheet.DayResponse day = allocation!.Days.Single(day => day.Date == date);
        Assert.Equal(5m, day.ProjectCells[firstAssignmentId].Hours);
        Assert.True(day.ProjectCells[firstAssignmentId].Locked);
    }

    [Fact]
    public async Task AllocateTimesheet_TopsUpPartialCoreToStagMinimum()
    {
        DateTime date = new(2036, 2, 4, 0, 0, 0, DateTimeKind.Utc);
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid assignmentId = Guid.CreateVersion7();
        await SeedSingleDayAsync(attendanceTimesheetId, date, AcademicEmployeeTypeId, assignmentId, assignmentWorkload: 0.75m);

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
                    Schedules: [new TimeRange(new TimeSpan(8, 0, 0), new TimeSpan(9, 50, 0))])
            ],
            Projects: [new ProjectColumnEdit(assignmentId, [new ProjectDayEdit(date, 0m)])]);

        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
        Assert.NotNull(allocation);
        AllocateTimesheet.DayResponse day = allocation!.Days.Single();
        Assert.Equal(2m, day.CoreHours);
        Assert.Equal(6m, day.ProjectCells[assignmentId].Hours);
    }

    [Fact]
    public async Task AllocateTimesheet_DoesNotGenerateNonAcademicAttendanceWhenMissing()
    {
        DateTime date = new(2036, 3, 3, 0, 0, 0, DateTimeKind.Utc);
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid assignmentId = Guid.CreateVersion7();
        await SeedSingleDayAsync(attendanceTimesheetId, date, NonAcademicEmployeeTypeId, assignmentId);

        TimesheetEditRequest request = new(
            Days: [new TimesheetDayEdit(Date: date, ClockIn: null, ClockOut: null, BreakStart: null, BreakEnd: null, CoreHours: 0m, Description: null, Schedules: [])],
            Projects: [new ProjectColumnEdit(assignmentId, [new ProjectDayEdit(date, 0m)])]);

        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate?day=3", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
        Assert.NotNull(allocation);
        AllocateTimesheet.DayResponse day = allocation!.Days.Single();
        Assert.Equal(new int?[] { null, null }, day.Work);
        Assert.Equal(new int?[] { null, null }, day.Break);
        Assert.Equal(0m, day.CoreHours);
        Assert.Equal(0m, day.ProjectCells[assignmentId].Hours);
    }

    [Theory]
    [InlineData(9, 6, 960, 720, 750, 1.5)]
    [InlineData(10, 8, 1050, 720, 750, 1)]
    public async Task AllocateTimesheet_PreservesAttendanceWhenLockedProjectIsBelowWorkedHours(int month, int lockedHours, int expectedClockOut, int? expectedBreakStart, int? expectedBreakEnd, decimal expectedCoreHours)
    {
        DateTime date = new(2036, month, 2, 0, 0, 0, DateTimeKind.Utc);
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid assignmentId = Guid.CreateVersion7();
        decimal workload = lockedHours / 8m;
        await SeedSingleDayAsync(attendanceTimesheetId, date, NonAcademicEmployeeTypeId, assignmentId, totalWorkload: workload, assignmentWorkload: workload);

        TimeSpan originalClockOut = lockedHours == 6 ? new TimeSpan(16, 0, 0) : new TimeSpan(17, 30, 0);
        TimesheetEditRequest request = new(
            Days:
            [
                new TimesheetDayEdit(Date: date, ClockIn: new TimeSpan(8, 0, 0), ClockOut: originalClockOut, BreakStart: new TimeSpan(12, 0, 0), BreakEnd: new TimeSpan(12, 30, 0), CoreHours: 0m, Description: null, Schedules: [])
            ],
            Projects: [new ProjectColumnEdit(assignmentId, [new ProjectDayEdit(date, lockedHours, HoursLocked: true)])]);

        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate?day=2", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
        Assert.NotNull(allocation);
        AllocateTimesheet.DayResponse day = allocation!.Days.Single();
        Assert.Equal(new int?[] { 480, expectedClockOut }, day.Work);
        Assert.Equal(new int?[] { expectedBreakStart, expectedBreakEnd }, day.Break);
        Assert.Equal(expectedCoreHours, day.CoreHours);
        Assert.Equal(lockedHours, day.ProjectCells[assignmentId].Hours);
        Assert.True(day.ProjectCells[assignmentId].Locked);
    }

    [Fact]
    public async Task AllocateTimesheet_DoesNotRaiseAttendanceWhenLockedProjectExceedsWorkedHours()
    {
        DateTime date = new(2036, 11, 5, 0, 0, 0, DateTimeKind.Utc);
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid assignmentId = Guid.CreateVersion7();
        await SeedSingleDayAsync(attendanceTimesheetId, date, NonAcademicEmployeeTypeId, assignmentId);

        TimesheetEditRequest request = new(
            Days:
            [
                new TimesheetDayEdit(Date: date, ClockIn: new TimeSpan(8, 0, 0), ClockOut: new TimeSpan(14, 0, 0), BreakStart: null, BreakEnd: null, CoreHours: 0m, Description: null, Schedules: [])
            ],
            Projects: [new ProjectColumnEdit(assignmentId, [new ProjectDayEdit(date, 8m, HoursLocked: true)])]);

        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate?day=5", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
        Assert.NotNull(allocation);
        AllocateTimesheet.DayResponse day = allocation!.Days.Single();
        Assert.Equal(new int?[] { 480, 840 }, day.Work);
        Assert.Equal(new int?[] { null, null }, day.Break);
        Assert.Equal(0m, day.CoreHours);
        Assert.Equal(8m, day.ProjectCells[assignmentId].Hours);
        Assert.False(day.AttendanceAdjusted);
    }

    [Fact]
    public async Task AllocateTimesheet_FillsAcademicMonthWithinTargetsAndDailyBounds()
    {
        int year = 2037;
        int month = 1;
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid assignmentId = Guid.CreateVersion7();
        await SeedMonthAsync(attendanceTimesheetId, assignmentId, year, month, totalWorkload: 1m, assignmentWorkload: 0.5m);
        DateTime[] dates = MonthDates(year, month);

        TimesheetEditRequest request = new(
            Days: dates.Select(date => new TimesheetDayEdit(Date: date, ClockIn: null, ClockOut: null, BreakStart: null, BreakEnd: null, CoreHours: 0m, Description: null, Schedules: [])).ToArray(),
            Projects: [new ProjectColumnEdit(assignmentId, dates.Select(date => new ProjectDayEdit(date, 0m)).ToArray())]);

        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
        Assert.NotNull(allocation);

        int weekdays = dates.Count(TimesheetLogic.IsWeekday);
        decimal expectedColumnTotal = TimesheetLogic.Normalize(weekdays * 8m * 0.5m);
        Assert.Equal(expectedColumnTotal, TimesheetLogic.Normalize(allocation!.Days.Sum(day => day.CoreHours)));
        Assert.Equal(expectedColumnTotal, TimesheetLogic.Normalize(allocation.Days.Sum(day => day.ProjectCells[assignmentId].Hours)));

        AllocateTimesheet.DayResponse[] activeDays = allocation.Days.Where(day => TimesheetLogic.IsWeekday(day.Date) && TimesheetLogic.Normalize(day.CoreHours + day.ProjectCells[assignmentId].Hours) > 0m).ToArray();
        Assert.NotEmpty(activeDays);
        Assert.True(activeDays.Length < weekdays);
        foreach (AllocateTimesheet.DayResponse day in activeDays)
        {
            Assert.InRange(TimesheetLogic.Normalize(day.CoreHours + day.ProjectCells[assignmentId].Hours), 6m, 12m);
        }

        IEnumerable<decimal> generatedCells = allocation.Days
            .SelectMany(day => new[] { day.CoreHours, day.ProjectCells[assignmentId].Hours })
            .Where(hours => hours > 0m);
        Assert.All(generatedCells, hours => Assert.True(hours >= 1m, $"Generated cell is shorter than 1 hour: {hours}"));
    }

    [Fact]
    public async Task AllocateTimesheet_AddsWeekdaysWhenStagDaysCannotFitMonthlyTargets()
    {
        int year = 2038;
        int month = 1;
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid assignmentId = Guid.CreateVersion7();
        await SeedMonthAsync(attendanceTimesheetId, assignmentId, year, month, totalWorkload: 0.1m, assignmentWorkload: 0.05m);
        DateTime[] dates = MonthDates(year, month);
        HashSet<DateTime> weekendStagDates = dates.Where(date => !TimesheetLogic.IsWeekday(date)).Take(2).ToHashSet();

        TimesheetEditRequest request = new(
            Days: dates.Select(date => new TimesheetDayEdit(
                Date: date,
                ClockIn: null,
                ClockOut: null,
                BreakStart: null,
                BreakEnd: null,
                CoreHours: 0m,
                Description: null,
                Schedules: weekendStagDates.Contains(date) ? [new TimeRange(new TimeSpan(9, 0, 0), new TimeSpan(10, 0, 0))] : [])).ToArray(),
            Projects: [new ProjectColumnEdit(assignmentId, dates.Select(date => new ProjectDayEdit(date, 0m)).ToArray())]);

        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
        Assert.NotNull(allocation);

        int weekdays = dates.Count(TimesheetLogic.IsWeekday);
        decimal expectedColumnTotal = TimesheetLogic.Normalize(weekdays * 8m * 0.05m);
        Assert.Equal(expectedColumnTotal, TimesheetLogic.Normalize(allocation!.Days.Sum(day => day.CoreHours)));
        Assert.Equal(expectedColumnTotal, TimesheetLogic.Normalize(allocation.Days.Sum(day => day.ProjectCells[assignmentId].Hours)));
    }

    [Fact]
    public async Task AllocateTimesheet_ReachesBothMonthlyTargetsForJanuaryWithInterruptions()
    {
        int year = 2043;
        int month = 1;
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid assignmentId = Guid.CreateVersion7();
        await SeedMonthAsync(attendanceTimesheetId, assignmentId, year, month, totalWorkload: 1m, assignmentWorkload: 0.5m);
        DateTime[] dates = MonthDates(year, month);
        Dictionary<int, string> interruptions = new()
        {
            [1] = "SCT",
            [7] = "SCT",
            [8] = "SCT",
            [9] = "SCT",
            [25] = "NL,N"
        };
        Dictionary<int, IReadOnlyList<TimeRange>> schedules = new()
        {
            [14] = [new TimeRange(new TimeSpan(9, 0, 0), new TimeSpan(9, 50, 0))],
            [16] = [new TimeRange(new TimeSpan(16, 0, 0), new TimeSpan(16, 50, 0))],
            [17] = [new TimeRange(new TimeSpan(9, 0, 0), new TimeSpan(9, 50, 0))],
            [19] = [new TimeRange(new TimeSpan(9, 0, 0), new TimeSpan(9, 50, 0))],
            [22] = [new TimeRange(new TimeSpan(16, 0, 0), new TimeSpan(16, 50, 0))]
        };
        TimesheetEditRequest request = new(
            Days: dates.Select(date => new TimesheetDayEdit(
                Date: date,
                ClockIn: null,
                ClockOut: null,
                BreakStart: null,
                BreakEnd: null,
                CoreHours: 0m,
                Description: interruptions.GetValueOrDefault(date.Day),
                Schedules: schedules.GetValueOrDefault(date.Day) ?? [])).ToArray(),
            Projects: [new ProjectColumnEdit(assignmentId, dates.Select(date => new ProjectDayEdit(date, 0m)).ToArray())]);

        for (int attempt = 0; attempt < 20; attempt++)
        {
            HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
            Assert.NotNull(allocation);

            Assert.Equal(88m, TimesheetLogic.Normalize(allocation!.Days.Sum(day => day.CoreHours)));
            Assert.Equal(88m, TimesheetLogic.Normalize(allocation.Days.Sum(day => day.ProjectCells[assignmentId].Hours)));
        }
    }

    [Fact]
    public async Task AllocateTimesheet_UsesExistingNonAcademicAttendanceAndProjectTargets()
    {
        int year = 2045;
        int month = 1;
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid firstAssignmentId = Guid.CreateVersion7();
        Guid secondAssignmentId = Guid.CreateVersion7();
        await SeedNonAcademicMonthAsync(attendanceTimesheetId, year, month, [(firstAssignmentId, 0.25m), (secondAssignmentId, 0.5m)]);
        DateTime[] dates = MonthDates(year, month);

        TimesheetEditRequest request = new(
            Days: dates.Select(date => ExistingWorkdayAttendance(date)).ToArray(),
            Projects:
            [
                new ProjectColumnEdit(firstAssignmentId, dates.Select(date => new ProjectDayEdit(date, 0m)).ToArray()),
                new ProjectColumnEdit(secondAssignmentId, dates.Select(date => new ProjectDayEdit(date, 0m)).ToArray())
            ]);

        for (int attempt = 0; attempt < 20; attempt++)
        {
            HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
            Assert.NotNull(allocation);

            Assert.Equal(176m, allocation!.Evaluation.Totals.WorkedHours);
            Assert.Equal(44m, allocation.Evaluation.Totals.CoreHours);
            Assert.Equal(44m, allocation.Evaluation.Totals.Projects.Single(project => project.ProjectId == firstAssignmentId).Hours);
            Assert.Equal(88m, allocation.Evaluation.Totals.Projects.Single(project => project.ProjectId == secondAssignmentId).Hours);
            AssertGeneratedNonAcademicCellsStayWithinBounds((AllocateTimesheet.Response)allocation);
        }
    }

    [Fact]
    public async Task AllocateTimesheet_UsesOvernightAttendanceWithoutChangingIt()
    {
        int year = 2051;
        int month = 1;
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid firstAssignmentId = Guid.CreateVersion7();
        Guid secondAssignmentId = Guid.CreateVersion7();
        await SeedNonAcademicMonthAsync(attendanceTimesheetId, year, month, [(firstAssignmentId, 0.25m), (secondAssignmentId, 0.5m)]);
        DateTime[] dates = MonthDates(year, month);
        DateTime overnightDate = dates.First(TimesheetLogic.IsWeekday);

        TimesheetEditRequest request = new(
            Days: dates.Select(date => new TimesheetDayEdit(
                Date: date,
                ClockIn: date == overnightDate ? new TimeSpan(22, 0, 0) : TimeSpan.Zero,
                ClockOut: date == overnightDate ? new TimeSpan(7, 0, 0) : TimeSpan.Zero,
                BreakStart: TimeSpan.Zero,
                BreakEnd: TimeSpan.Zero,
                CoreHours: 0m,
                Description: null,
                Schedules: [])).ToArray(),
            Projects:
            [
                new ProjectColumnEdit(firstAssignmentId, dates.Select(date => new ProjectDayEdit(date, 0m)).ToArray()),
                new ProjectColumnEdit(secondAssignmentId, dates.Select(date => new ProjectDayEdit(date, 0m)).ToArray())
            ]);

        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
        Assert.NotNull(allocation);

        Assert.Equal(9m, allocation!.Evaluation.Totals.WorkedHours);
        Assert.Equal(0m, allocation.Evaluation.Totals.CoreHours);
        Assert.Equal(3m, allocation.Evaluation.Totals.Projects.Single(project => project.ProjectId == firstAssignmentId).Hours);
        Assert.Equal(6m, allocation.Evaluation.Totals.Projects.Single(project => project.ProjectId == secondAssignmentId).Hours);
        AllocateTimesheet.DayResponse overnight = allocation.Days.Single(day => day.Date == overnightDate);
        Assert.Equal(new int?[] { 1320, 420 }, overnight.Work);
        AssertGeneratedNonAcademicCellsStayWithinBounds((AllocateTimesheet.Response)allocation);
    }

    [Fact]
    public async Task AllocateTimesheet_RebuildsNonAcademicMonthFromBadGeneratedInput()
    {
        int year = 2054;
        int month = 1;
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid firstAssignmentId = Guid.CreateVersion7();
        Guid secondAssignmentId = Guid.CreateVersion7();
        await SeedNonAcademicMonthAsync(attendanceTimesheetId, year, month, [(firstAssignmentId, 0.25m), (secondAssignmentId, 0.5m)]);
        DateTime[] dates = MonthDates(year, month);

        TimesheetEditRequest request = new(
            Days: dates.Select(date => ExistingWorkdayAttendance(date, coreHours: date.Day % 3 == 0 ? 5m : 0m)).ToArray(),
            Projects:
            [
                new ProjectColumnEdit(firstAssignmentId, dates.Select(date => new ProjectDayEdit(date, date.Day % 2 == 0 ? 2m : 0m)).ToArray()),
                new ProjectColumnEdit(secondAssignmentId, dates.Select(date => new ProjectDayEdit(date, date.Day % 2 == 1 ? 7m : 0m)).ToArray())
            ]);

        for (int attempt = 0; attempt < 50; attempt++)
        {
            HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
            Assert.NotNull(allocation);

            Assert.Equal(176m, allocation!.Evaluation.Totals.WorkedHours);
            Assert.Equal(44m, allocation.Evaluation.Totals.CoreHours);
            Assert.Equal(44m, allocation.Evaluation.Totals.Projects.Single(project => project.ProjectId == firstAssignmentId).Hours);
            Assert.Equal(88m, allocation.Evaluation.Totals.Projects.Single(project => project.ProjectId == secondAssignmentId).Hours);
            AssertGeneratedNonAcademicCellsStayWithinBounds((AllocateTimesheet.Response)allocation);
        }
    }

    [Fact]
    public async Task AllocateTimesheet_GeneratesNonAcademicCoreOnlyMonthFromExistingAttendance()
    {
        int year = 2056;
        int month = 1;
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        await SeedNonAcademicMonthAsync(attendanceTimesheetId, year, month, []);
        DateTime[] dates = MonthDates(year, month);
        decimal expected = dates.Count(TimesheetLogic.IsWeekday) * 8m;

        TimesheetEditRequest request = new(
            Days: dates.Select(date => ExistingWorkdayAttendance(date)).ToArray(),
            Projects: []);

        for (int attempt = 0; attempt < 25; attempt++)
        {
            HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
            Assert.NotNull(allocation);

            Assert.Equal(expected, allocation!.Evaluation.Totals.WorkedHours);
            Assert.Equal(expected, allocation.Evaluation.Totals.CoreHours);
            Assert.Empty(allocation.Evaluation.Totals.Projects);
            AssertGeneratedNonAcademicCellsStayWithinBounds((AllocateTimesheet.Response)allocation);
        }
    }

    [Fact]
    public async Task AllocateTimesheet_GeneratesNonAcademicMonthWithSeveralProjectWorkloads()
    {
        int year = 2058;
        int month = 1;
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid firstAssignmentId = Guid.CreateVersion7();
        Guid secondAssignmentId = Guid.CreateVersion7();
        Guid thirdAssignmentId = Guid.CreateVersion7();
        await SeedNonAcademicMonthAsync(attendanceTimesheetId, year, month, [(firstAssignmentId, 0.10m), (secondAssignmentId, 0.15m), (thirdAssignmentId, 0.25m)]);
        DateTime[] dates = MonthDates(year, month);
        decimal total = dates.Count(TimesheetLogic.IsWeekday) * 8m;

        TimesheetEditRequest request = new(
            Days: dates.Select(date => ExistingWorkdayAttendance(date)).ToArray(),
            Projects:
            [
                new ProjectColumnEdit(firstAssignmentId, dates.Select(date => new ProjectDayEdit(date, 0m)).ToArray()),
                new ProjectColumnEdit(secondAssignmentId, dates.Select(date => new ProjectDayEdit(date, 0m)).ToArray()),
                new ProjectColumnEdit(thirdAssignmentId, dates.Select(date => new ProjectDayEdit(date, 0m)).ToArray())
            ]);

        for (int attempt = 0; attempt < 25; attempt++)
        {
            HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
            Assert.NotNull(allocation);

            Assert.Equal(total, allocation!.Evaluation.Totals.WorkedHours);
            Assert.Equal(TimesheetLogic.Normalize(total - allocation.Evaluation.Totals.Projects.Sum(project => project.Hours)), allocation.Evaluation.Totals.CoreHours);
            Assert.Equal(TimesheetLogic.Normalize(total * 0.10m), allocation.Evaluation.Totals.Projects.Single(project => project.ProjectId == firstAssignmentId).Hours);
            Assert.Equal(TimesheetLogic.Normalize(total * 0.15m), allocation.Evaluation.Totals.Projects.Single(project => project.ProjectId == secondAssignmentId).Hours);
            Assert.Equal(TimesheetLogic.Normalize(total * 0.25m), allocation.Evaluation.Totals.Projects.Single(project => project.ProjectId == thirdAssignmentId).Hours);
            AssertGeneratedNonAcademicCellsStayWithinBounds((AllocateTimesheet.Response)allocation);
        }
    }


    [Fact]
    public async Task AllocateTimesheet_GeneratesNonAcademicMonthWithDecimalProjectWorkload()
    {
        int year = 2060;
        int month = 1;
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid assignmentId = Guid.CreateVersion7();
        await SeedNonAcademicMonthAsync(attendanceTimesheetId, year, month, [(assignmentId, 0.1075m)]);
        DateTime[] dates = MonthDates(year, month);
        decimal total = dates.Count(TimesheetLogic.IsWeekday) * 8m;
        decimal projectTarget = TimesheetLogic.Normalize(total * 0.1075m);

        TimesheetEditRequest request = new(
            Days: dates.Select(date => ExistingWorkdayAttendance(date)).ToArray(),
            Projects: [new ProjectColumnEdit(assignmentId, dates.Select(date => new ProjectDayEdit(date, 0m)).ToArray())]);

        for (int attempt = 0; attempt < 25; attempt++)
        {
            HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
            Assert.NotNull(allocation);

            Assert.Equal(total, allocation!.Evaluation.Totals.WorkedHours);
            Assert.Equal(TimesheetLogic.Normalize(total - projectTarget), allocation.Evaluation.Totals.CoreHours);
            Assert.Equal(projectTarget, allocation.Evaluation.Totals.Projects.Single(project => project.ProjectId == assignmentId).Hours);
            AssertGeneratedNonAcademicCellsStayWithinBounds((AllocateTimesheet.Response)allocation);
        }
    }

    [Fact]
    public async Task AllocateTimesheet_DoesNotFailWhenLockedProjectsLeaveTinyRemainder()
    {
        int year = 2063;
        int month = 3;
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid assignmentId = Guid.CreateVersion7();
        await SeedNonAcademicMonthAsync(attendanceTimesheetId, year, month, [(assignmentId, 0.10m)]);
        DateTime[] dates = MonthDates(year, month);
        DateTime[] workdays = dates.Where(TimesheetLogic.IsWeekday).Take(2).ToArray();

        TimesheetEditRequest request = new(
            Days: dates.Select(date => new TimesheetDayEdit(date, null, null, null, null, 0m, null, [])).ToArray(),
            Projects:
            [
                new ProjectColumnEdit(assignmentId, dates.Select(date =>
                    date == workdays[0] ? new ProjectDayEdit(date, 5m, HoursLocked: true) :
                    date == workdays[1] ? new ProjectDayEdit(date, 8m, HoursLocked: true) :
                    new ProjectDayEdit(date, 0m)).ToArray())
            ]);

        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
        Assert.NotNull(allocation);
        Assert.Equal(13m, TimesheetLogic.Normalize(allocation!.Days.Sum(day => day.ProjectCells[assignmentId].Hours)));
    }

    [Fact]
    public async Task AllocateTimesheet_GeneratesOneDecimalProjectCellForExactNonAcademicMonthlyTarget()
    {
        int year = 2064;
        int month = 2;
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid assignmentId = Guid.CreateVersion7();
        await SeedNonAcademicMonthAsync(attendanceTimesheetId, year, month, [(assignmentId, 0.3m)]);
        DateTime[] dates = MonthDates(year, month);
        decimal total = dates.Count(TimesheetLogic.IsWeekday) * 8m;
        decimal projectTarget = TimesheetLogic.Normalize(total * 0.3m);

        TimesheetEditRequest request = new(
            Days: dates.Select(date => ExistingWorkdayAttendance(date)).ToArray(),
            Projects: [new ProjectColumnEdit(assignmentId, dates.Select(date => new ProjectDayEdit(date, 0m)).ToArray())]);

        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
        Assert.NotNull(allocation);
        decimal projectHours = TimesheetLogic.Normalize(allocation!.Days.Sum(day => day.ProjectCells[assignmentId].Hours));
        Assert.Equal(168m, total);
        Assert.Equal(50.4m, projectTarget);
        Assert.Equal(projectTarget, projectHours);
        Assert.Single(allocation.Days.Where(day => day.ProjectCells[assignmentId].Hours > 0m && day.ProjectCells[assignmentId].Hours != HalfHour(day.ProjectCells[assignmentId].Hours)));
        Assert.DoesNotContain(allocation.Evaluation.Issues, issue => issue.Code == "ERR-COM-06");
    }

    [Fact]
    public async Task AllocateTimesheet_GeneratesNonAcademicMonthWithProportionalInterruption()
    {
        int year = 2062;
        int month = 1;
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid assignmentId = Guid.CreateVersion7();
        await SeedNonAcademicMonthAsync(attendanceTimesheetId, year, month, [(assignmentId, 0.5m)]);
        DateTime[] dates = MonthDates(year, month);
        DateTime interruptionDate = dates.First(TimesheetLogic.IsWeekday);
        decimal total = dates.Count(TimesheetLogic.IsWeekday) * 8m;
        decimal columnTarget = TimesheetLogic.Normalize(total * 0.5m);

        TimesheetEditRequest request = new(
            Days: dates.Select(date => ExistingWorkdayAttendance(date, description: date == interruptionDate ? "D" : null)).ToArray(),
            Projects: [new ProjectColumnEdit(assignmentId, dates.Select(date => new ProjectDayEdit(date, 0m)).ToArray())]);

        for (int attempt = 0; attempt < 25; attempt++)
        {
            HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
            Assert.NotNull(allocation);

            Assert.Equal(total, allocation!.Evaluation.Totals.WorkedHours);
            Assert.Equal(columnTarget, allocation.Evaluation.Totals.CoreHours);
            Assert.Equal(columnTarget, allocation.Evaluation.Totals.Projects.Single(project => project.ProjectId == assignmentId).Hours);

            AllocateTimesheet.DayResponse interruption = allocation.Days.Single(day => day.Date == interruptionDate);
            Assert.Equal(4m, interruption.CoreHours);
            Assert.Equal(4m, interruption.ProjectCells[assignmentId].Hours);
            AssertGeneratedNonAcademicCellsStayWithinBounds((AllocateTimesheet.Response)allocation, interruptionDate);
        }
    }
    [Fact]
    public async Task AllocateTimesheet_KeepsFixedCoreAndProjectHours()
    {
        DateTime date = new(2036, 5, 2, 0, 0, 0, DateTimeKind.Utc);
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid assignmentId = Guid.CreateVersion7();
        await SeedSingleDayAsync(attendanceTimesheetId, date, AcademicEmployeeTypeId, assignmentId, assignmentWorkload: 0.5m);

        TimesheetEditRequest request = new(
            Days: [new TimesheetDayEdit(Date: date, ClockIn: null, ClockOut: null, BreakStart: null, BreakEnd: null, CoreHours: 1.83m, Description: null, Schedules: [new TimeRange(new TimeSpan(16, 0, 0), new TimeSpan(16, 50, 0))], CoreHoursFixed: true)],
            Projects: [new ProjectColumnEdit(assignmentId, [new ProjectDayEdit(date, 3m, HoursLocked: true)])]);

        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate?day=2", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
        Assert.NotNull(allocation);
        AllocateTimesheet.DayResponse day = allocation!.Days.Single();
        Assert.Equal(1.83m, day.CoreHours);
        Assert.Equal(3m, day.ProjectCells[assignmentId].Hours);
        Assert.True(day.ProjectCells[assignmentId].Locked);
    }

    [Fact]
    public async Task AllocateTimesheet_DoesNotGenerateMissingNonAcademicAttendanceFromStag()
    {
        DateTime date = new(2036, 6, 2, 0, 0, 0, DateTimeKind.Utc);
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        await SeedSingleDayAsync(attendanceTimesheetId, date, NonAcademicEmployeeTypeId, assignmentId: null);

        TimesheetEditRequest request = new(
            Days: [new TimesheetDayEdit(Date: date, ClockIn: null, ClockOut: null, BreakStart: null, BreakEnd: null, CoreHours: 0m, Description: null, Schedules: [new TimeRange(new TimeSpan(16, 50, 0), new TimeSpan(17, 50, 0))])],
            Projects: []);

        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate?day=2", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
        Assert.NotNull(allocation);
        AllocateTimesheet.DayResponse day = allocation!.Days.Single();
        Assert.Equal(new int?[] { null, null }, day.Work);
        Assert.Equal(new int?[] { null, null }, day.Break);
        Assert.Equal(0m, day.CoreHours);
    }

    [Fact]
    public async Task AllocateTimesheet_FillsWeekendStagCoreOnSingleDay()
    {
        DateTime date = new(2026, 1, 17, 0, 0, 0, DateTimeKind.Utc);
        Assert.False(TimesheetLogic.IsWeekday(date));
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid assignmentId = Guid.CreateVersion7();
        await SeedSingleDayAsync(attendanceTimesheetId, date, AcademicEmployeeTypeId, assignmentId, assignmentWorkload: 0.25m);

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
                    Schedules: [new TimeRange(new TimeSpan(9, 0, 0), new TimeSpan(12, 50, 0))])
            ],
            Projects: [new ProjectColumnEdit(assignmentId, [new ProjectDayEdit(date, 0m)])]);

        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate?day=17", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
        Assert.NotNull(allocation);
        AllocateTimesheet.DayResponse day = allocation!.Days.Single();
        Assert.True(day.CoreHours >= 3.83m);
        Assert.DoesNotContain(allocation.Evaluation.DayIssues, issue => issue.Code == "ERR-ALL-02" && issue.Day == 17);
    }

    [Fact]
    public async Task AllocateTimesheet_DoesNotGenerateMissingNonAcademicAttendanceWithBreak()
    {
        DateTime date = new(2036, 7, 2, 0, 0, 0, DateTimeKind.Utc);
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        await SeedSingleDayAsync(attendanceTimesheetId, date, NonAcademicEmployeeTypeId, assignmentId: null);

        TimesheetEditRequest request = new(
            Days: [new TimesheetDayEdit(Date: date, ClockIn: null, ClockOut: null, BreakStart: null, BreakEnd: null, CoreHours: 7m, Description: null, Schedules: [], CoreHoursFixed: true)],
            Projects: []);

        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate?day=2", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
        Assert.NotNull(allocation);
        AllocateTimesheet.DayResponse day = allocation!.Days.Single();
        Assert.Equal(new int?[] { null, null }, day.Work);
        Assert.Equal(new int?[] { null, null }, day.Break);
        Assert.Equal(7m, day.CoreHours);
    }

    private static void AssertGeneratedNonAcademicCellsStayWithinBounds(AllocateTimesheet.Response allocation, params DateTime[] ignoredDates)
    {
        HashSet<DateTime> ignored = ignoredDates.ToHashSet();
        foreach (AllocateTimesheet.DayResponse day in allocation.Days)
        {
            if (ignored.Contains(day.Date))
            {
                continue;
            }

            decimal total = TimesheetLogic.Normalize(day.CoreHours + day.ProjectCells.Values.Sum(cell => cell.Hours));
            if (total > 0m)
            {
                decimal worked = ResponseWorkedHours(day);
                Assert.True(total <= Math.Min(12m, worked) + 0.009m, $"Generated total {total} exceeds attendance {worked} on {day.Date:yyyy-MM-dd}");
            }

        }

        foreach (IGrouping<Guid, KeyValuePair<Guid, AllocateTimesheet.ProjectCell>> projectCells in allocation.Days.SelectMany(day => day.ProjectCells).GroupBy(cell => cell.Key))
        {
            Assert.True(projectCells.Count(cell => cell.Value.Hours > 0m && cell.Value.Hours != HalfHour(cell.Value.Hours)) <= 1);
        }
    }

    private static decimal ResponseWorkedHours(AllocateTimesheet.DayResponse day) =>
        TimesheetLogic.CalculateWorkedHoursFromAttendance(ToTime(day.Work[0]), ToTime(day.Work[1]), ToTime(day.Break[0]), ToTime(day.Break[1]));

    private static TimeSpan? ToTime(int? minutes) => minutes.HasValue ? TimeSpan.FromMinutes(minutes.Value) : null;

    private async Task SeedAsync(Guid attendanceTimesheetId, Guid firstAssignmentId, Guid secondAssignmentId, DateTime firstDate)
    {
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        ContractEmployee firstAssignment = Assignment(firstAssignmentId, "INT-1", $"Interruption 1 {firstAssignmentId}", firstDate);
        ContractEmployee secondAssignment = Assignment(secondAssignmentId, "INT-2", $"Interruption 2 {secondAssignmentId}", firstDate);
        dbContext.ContractEmployees.AddRange(firstAssignment, secondAssignment);
        dbContext.EmployeeWorkloads.Add(new EmployeeWorkload { Id = Guid.CreateVersion7(), EmployeeId = SeededTestData.JanNovakEmployeeId, Year = firstDate.Year, Month = firstDate.Month, Workload = 1m });
        dbContext.AttendanceTimesheets.Add(new Data.Models.AttendanceTimesheet
        {
            Id = attendanceTimesheetId,
            EmployeeId = SeededTestData.JanNovakEmployeeId,
            EmployeeTypeId = AcademicEmployeeTypeId,
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

    private async Task SeedSingleDayAsync(Guid attendanceTimesheetId, DateTime date, Guid employeeTypeId, Guid? assignmentId, decimal assignmentWorkload = 0.5m, decimal totalWorkload = 1m)
    {
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Employee employee = await dbContext.Employees.SingleAsync(employee => employee.Id == SeededTestData.JanNovakEmployeeId);
        employee.EmployeeTypeId = employeeTypeId;
        dbContext.EmployeeWorkloads.Add(new EmployeeWorkload { Id = Guid.CreateVersion7(), EmployeeId = SeededTestData.JanNovakEmployeeId, Year = date.Year, Month = date.Month, Workload = totalWorkload });
        dbContext.AttendanceTimesheets.Add(new Data.Models.AttendanceTimesheet
        {
            Id = attendanceTimesheetId,
            EmployeeId = SeededTestData.JanNovakEmployeeId,
            EmployeeTypeId = employeeTypeId,
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
                Id = Guid.CreateVersion7(),
                EmployeeId = SeededTestData.JanNovakEmployeeId,
                ContractId = SeededTestData.BetaContractId,
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

    private async Task SeedMonthAsync(Guid attendanceTimesheetId, Guid assignmentId, int year, int month, decimal totalWorkload, decimal assignmentWorkload)
    {
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Employee employee = await dbContext.Employees.SingleAsync(employee => employee.Id == SeededTestData.JanNovakEmployeeId);
        employee.EmployeeTypeId = AcademicEmployeeTypeId;
        DateTime[] dates = MonthDates(year, month);
        DateTime firstDate = dates[0];
        DateTime lastDate = dates[^1];

        dbContext.EmployeeWorkloads.Add(new EmployeeWorkload { Id = Guid.CreateVersion7(), EmployeeId = SeededTestData.JanNovakEmployeeId, Year = year, Month = month, Workload = totalWorkload });
        dbContext.ContractEmployees.Add(Assignment(assignmentId, $"MONTH-{year}-{month}", $"Month {year}-{month}", firstDate, assignmentWorkload, lastDate));
        dbContext.AttendanceTimesheets.Add(new Data.Models.AttendanceTimesheet
        {
            Id = attendanceTimesheetId,
            EmployeeId = SeededTestData.JanNovakEmployeeId,
            EmployeeTypeId = AcademicEmployeeTypeId,
            TimesheetStatusId = TestTimesheetStatusIds.Draft,
            Year = year,
            Month = month,
            Days = dates.Select(date => AttendanceDay(date, null)).ToList()
        });
        dbContext.ProjectTimesheets.Add(new Data.Models.ProjectTimesheet
        {
            Id = Guid.CreateVersion7(),
            EmployeeId = SeededTestData.JanNovakEmployeeId,
            ContractId = SeededTestData.BetaContractId,
            ContractEmployeeId = assignmentId,
            TimesheetStatusId = TestTimesheetStatusIds.Draft,
            Year = year,
            Month = month,
            Workload = assignmentWorkload,
            Days = dates.Select(date => ProjectDay(date, assignmentWorkload)).ToList()
        });

        await dbContext.SaveChangesAsync();
    }

    private async Task SeedNonAcademicMonthAsync(Guid attendanceTimesheetId, int year, int month, IReadOnlyList<(Guid Id, decimal Workload)> assignments)
    {
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Employee employee = await dbContext.Employees.SingleAsync(employee => employee.Id == SeededTestData.JanNovakEmployeeId);
        employee.EmployeeTypeId = NonAcademicEmployeeTypeId;
        DateTime[] dates = MonthDates(year, month);
        DateTime firstDate = dates[0];
        DateTime lastDate = dates[^1];

        dbContext.EmployeeWorkloads.Add(new EmployeeWorkload { Id = Guid.CreateVersion7(), EmployeeId = SeededTestData.JanNovakEmployeeId, Year = year, Month = month, Workload = 1m });
        dbContext.AttendanceTimesheets.Add(new Data.Models.AttendanceTimesheet
        {
            Id = attendanceTimesheetId,
            EmployeeId = SeededTestData.JanNovakEmployeeId,
            EmployeeTypeId = NonAcademicEmployeeTypeId,
            TimesheetStatusId = TestTimesheetStatusIds.Draft,
            Year = year,
            Month = month,
            Days = dates.Select(date => AttendanceDay(date, null)).ToList()
        });

        for (int index = 0; index < assignments.Count; index++)
        {
            (Guid id, decimal workload) = assignments[index];
            dbContext.ContractEmployees.Add(Assignment(id, $"NONACA-{year}-{month}-{index}", $"Non-academic {year}-{month}-{index}", firstDate, workload, lastDate));
            dbContext.ProjectTimesheets.Add(new Data.Models.ProjectTimesheet
            {
                Id = Guid.CreateVersion7(),
                EmployeeId = SeededTestData.JanNovakEmployeeId,
                ContractId = SeededTestData.BetaContractId,
                ContractEmployeeId = id,
                TimesheetStatusId = TestTimesheetStatusIds.Draft,
                Year = year,
                Month = month,
                Workload = workload,
                Days = dates.Select(date => ProjectDay(date, workload)).ToList()
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static decimal HalfHour(decimal value) => TimesheetLogic.Normalize(Math.Round(value * 2m, MidpointRounding.AwayFromZero) / 2m);

    private static TimesheetDayEdit ExistingWorkdayAttendance(DateTime date, decimal coreHours = 0m, string? description = null, IReadOnlyList<TimeRange>? schedules = null) =>
        TimesheetLogic.IsWeekday(date)
            ? new TimesheetDayEdit(date, new TimeSpan(8, 0, 0), new TimeSpan(16, 0, 0), null, null, coreHours, description, schedules ?? [])
            : new TimesheetDayEdit(date, null, null, null, null, coreHours, description, schedules ?? []);

    private static ContractEmployee Assignment(Guid id, string code, string position, DateTime date, decimal workload = 0.25m, DateTime? endDate = null) => new()
    {
        Id = id,
        ContractId = SeededTestData.BetaContractId,
        EmployeeId = SeededTestData.JanNovakEmployeeId,
        PositionCode = code,
        Position = position,
        Workload = workload,
        StartDate = date,
        EndDate = endDate ?? date.AddDays(2)
    };

    private static Data.Models.AttendanceDay AttendanceDay(DateTime date, string? description) => new() { Id = Guid.CreateVersion7(), Date = date, Workload = 1m, HoursObligation = 8m, Description = description, Schedules = "[]" };

    private static Data.Models.ProjectTimesheet ProjectTimesheet(Guid assignmentId, DateTime firstDate) => new()
    {
        Id = Guid.CreateVersion7(),
        EmployeeId = SeededTestData.JanNovakEmployeeId,
        ContractId = SeededTestData.BetaContractId,
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

    private static Data.Models.ProjectDay ProjectDay(DateTime date, decimal workload = 0.25m) => new() { Id = Guid.CreateVersion7(), Date = date, Workload = workload, HoursObligation = 8m * workload };
    private static TimesheetDayEdit Day(DateTime date, string description) => new(Date: date, ClockIn: null, ClockOut: null, BreakStart: null, BreakEnd: null, CoreHours: 0m, Description: description, Schedules: []);
    private static ProjectColumnEdit Project(Guid assignmentId, DateTime firstDate) => new(ContractEmployeeId: assignmentId, Days: [new ProjectDayEdit(firstDate, 0m), new ProjectDayEdit(firstDate.AddDays(1), 0m), new ProjectDayEdit(firstDate.AddDays(2), 0m)]);
    private static DateTime[] MonthDates(int year, int month) => Enumerable.Range(1, DateTime.DaysInMonth(year, month)).Select(day => new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc)).ToArray();
}
