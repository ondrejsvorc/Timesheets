using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;
using Timesheets.Api.Timesheets;
using Timesheets.Api.Timesheets.Endpoints;

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
        Assert.Equal(2m, doctor.ProjectHours[firstAssignmentId]);
        Assert.Equal(2m, doctor.ProjectHours[secondAssignmentId]);

        AllocateTimesheet.DayResponse proportional = allocation.Days.Single(day => day.Date == firstDate.AddDays(1));
        Assert.Equal(4m, proportional.CoreHours);
        Assert.Equal(2m, proportional.ProjectHours[firstAssignmentId]);
        Assert.Equal(2m, proportional.ProjectHours[secondAssignmentId]);

        AllocateTimesheet.DayResponse businessTrip = allocation.Days.Single(day => day.Date == firstDate.AddDays(2));
        Assert.Equal(0m, businessTrip.CoreHours);
        Assert.All(businessTrip.ProjectHours.Values, hours => Assert.Equal(0m, hours));
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
        Assert.Equal(6m, day.ProjectHours[assignmentId]);
    }

    [Fact]
    public async Task AllocateTimesheet_GeneratesNonAcademicAttendanceAndAllocationWhenMissing()
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
        Assert.Equal(new int?[] { 480, 990 }, day.Work);
        Assert.Equal(new int?[] { 720, 750 }, day.Break);
        Assert.Equal(4m, day.CoreHours);
        Assert.Equal(4m, day.ProjectHours[assignmentId]);
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
        Assert.Equal(expectedColumnTotal, TimesheetLogic.Normalize(allocation.Days.Sum(day => day.ProjectHours[assignmentId])));

        AllocateTimesheet.DayResponse[] activeDays = allocation.Days.Where(day => TimesheetLogic.IsWeekday(day.Date) && TimesheetLogic.Normalize(day.CoreHours + day.ProjectHours[assignmentId]) > 0m).ToArray();
        Assert.NotEmpty(activeDays);
        Assert.True(activeDays.Length < weekdays);
        foreach (AllocateTimesheet.DayResponse day in activeDays)
        {
            Assert.InRange(TimesheetLogic.Normalize(day.CoreHours + day.ProjectHours[assignmentId]), 6m, 12m);
        }

        IEnumerable<decimal> generatedCells = allocation.Days
            .SelectMany(day => new[] { day.CoreHours, day.ProjectHours[assignmentId] })
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
        Assert.Equal(expectedColumnTotal, TimesheetLogic.Normalize(allocation.Days.Sum(day => day.ProjectHours[assignmentId])));
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
            Assert.Equal(88m, TimesheetLogic.Normalize(allocation.Days.Sum(day => day.ProjectHours[assignmentId])));
        }
    }

    [Fact]
    public async Task AllocateTimesheet_ReachesNonAcademicAttendanceAndProjectTargets()
    {
        int year = 2045;
        int month = 1;
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        Guid firstAssignmentId = Guid.CreateVersion7();
        Guid secondAssignmentId = Guid.CreateVersion7();
        await SeedNonAcademicMonthAsync(attendanceTimesheetId, year, month, [(firstAssignmentId, 0.25m), (secondAssignmentId, 0.5m)]);
        DateTime[] dates = MonthDates(year, month);

        TimesheetEditRequest request = new(
            Days: dates.Select(date =>
            {
                bool filled = TimesheetLogic.IsWeekday(date) && date.Day <= 15;
                return new TimesheetDayEdit(
                    Date: date,
                    ClockIn: filled ? new TimeSpan(8, 0, 0) : TimeSpan.Zero,
                    ClockOut: filled ? new TimeSpan(16, 0, 0) : TimeSpan.Zero,
                    BreakStart: TimeSpan.Zero,
                    BreakEnd: TimeSpan.Zero,
                    CoreHours: 0m,
                    Description: null,
                    Schedules: []);
            }).ToArray(),
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
            AssertGeneratedNonAcademicCellsStayWithinBounds(allocation);
        }
    }

    [Fact]
    public async Task AllocateTimesheet_DoesNotBreakOvernightAttendanceWhenCompletingNonAcademicMonth()
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

        Assert.Equal(176m, allocation!.Evaluation.Totals.WorkedHours);
        Assert.Equal(44m, allocation.Evaluation.Totals.CoreHours);
        Assert.Equal(44m, allocation.Evaluation.Totals.Projects.Single(project => project.ProjectId == firstAssignmentId).Hours);
        Assert.Equal(88m, allocation.Evaluation.Totals.Projects.Single(project => project.ProjectId == secondAssignmentId).Hours);
        AssertGeneratedNonAcademicCellsStayWithinBounds(allocation);
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
            Days: dates.Select(date => new TimesheetDayEdit(
                Date: date,
                ClockIn: TimesheetLogic.IsWeekday(date) ? new TimeSpan(7, 0, 0) : new TimeSpan(17, 25, 0),
                ClockOut: TimesheetLogic.IsWeekday(date) ? new TimeSpan(18, 30, 0) : new TimeSpan(17, 27, 0),
                BreakStart: TimesheetLogic.IsWeekday(date) ? new TimeSpan(10, 45, 0) : TimeSpan.Zero,
                BreakEnd: TimesheetLogic.IsWeekday(date) ? new TimeSpan(11, 15, 0) : TimeSpan.Zero,
                CoreHours: date.Day % 3 == 0 ? 5m : 0m,
                Description: null,
                Schedules: [])).ToArray(),
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
            AssertGeneratedNonAcademicCellsStayWithinBounds(allocation);
        }
    }

    [Fact]
    public async Task AllocateTimesheet_GeneratesNonAcademicCoreOnlyMonth()
    {
        int year = 2056;
        int month = 1;
        Guid attendanceTimesheetId = Guid.CreateVersion7();
        await SeedNonAcademicMonthAsync(attendanceTimesheetId, year, month, []);
        DateTime[] dates = MonthDates(year, month);
        decimal expected = dates.Count(TimesheetLogic.IsWeekday) * 8m;

        TimesheetEditRequest request = new(
            Days: dates.Select(date => new TimesheetDayEdit(date, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, 0m, null, [])).ToArray(),
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
            AssertGeneratedNonAcademicCellsStayWithinBounds(allocation);
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
            Days: dates.Select(date => new TimesheetDayEdit(date, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, 0m, null, [])).ToArray(),
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
            Assert.Equal(TimesheetLogic.Normalize(total * 0.50m), allocation.Evaluation.Totals.CoreHours);
            Assert.Equal(TimesheetLogic.Normalize(total * 0.10m), allocation.Evaluation.Totals.Projects.Single(project => project.ProjectId == firstAssignmentId).Hours);
            Assert.Equal(TimesheetLogic.Normalize(total * 0.15m), allocation.Evaluation.Totals.Projects.Single(project => project.ProjectId == secondAssignmentId).Hours);
            Assert.Equal(TimesheetLogic.Normalize(total * 0.25m), allocation.Evaluation.Totals.Projects.Single(project => project.ProjectId == thirdAssignmentId).Hours);
            AssertGeneratedNonAcademicCellsStayWithinBounds(allocation);
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
            Days: dates.Select(date => new TimesheetDayEdit(date, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, 0m, null, [])).ToArray(),
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
            AssertGeneratedNonAcademicCellsStayWithinBounds(allocation);
        }
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
            Days: dates.Select(date => new TimesheetDayEdit(
                Date: date,
                ClockIn: TimeSpan.Zero,
                ClockOut: TimeSpan.Zero,
                BreakStart: TimeSpan.Zero,
                BreakEnd: TimeSpan.Zero,
                CoreHours: 0m,
                Description: date == interruptionDate ? "D" : null,
                Schedules: [])).ToArray(),
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
            Assert.Equal(4m, interruption.ProjectHours[assignmentId]);
            AssertGeneratedNonAcademicCellsStayWithinBounds(allocation, interruptionDate);
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
            Projects: [new ProjectColumnEdit(assignmentId, [new ProjectDayEdit(date, 3m, HoursFixed: true)])]);

        HttpResponseMessage response = await Client.PostAsJsonAsync($"/api/timesheets/{attendanceTimesheetId}/allocate?day=2", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AllocateTimesheet.Response? allocation = await response.Content.ReadFromJsonAsync<AllocateTimesheet.Response>();
        Assert.NotNull(allocation);
        AllocateTimesheet.DayResponse day = allocation!.Days.Single();
        Assert.Equal(1.83m, day.CoreHours);
        Assert.Equal(3m, day.ProjectHours[assignmentId]);
    }

    [Fact]
    public async Task AllocateTimesheet_GeneratesMissingAttendanceFromStag()
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
        Assert.Equal(new int?[] { 1010, 1070 }, day.Work);
        Assert.Equal(new int?[] { null, null }, day.Break);
        Assert.Equal(1m, day.CoreHours);
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
    public async Task AllocateTimesheet_GeneratesMissingAttendanceWithBreak()
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
        Assert.Equal(new int?[] { 420, 870 }, day.Work);
        Assert.Equal(new int?[] { 660, 690 }, day.Break);
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

            decimal total = TimesheetLogic.Normalize(day.CoreHours + day.ProjectHours.Values.Sum());
            if (total > 0m)
            {
                Assert.InRange(total, 6m, 12m);
            }
            if (day.CoreHours > 0m)
            {
                Assert.InRange(day.CoreHours, 6m, 12m);
            }

            foreach (decimal hours in day.ProjectHours.Values.Where(hours => hours > 0m))
            {
                Assert.InRange(hours, 6m, 12m);
            }
        }
    }

    private async Task SeedAsync(Guid attendanceTimesheetId, Guid firstAssignmentId, Guid secondAssignmentId, DateTime firstDate)
    {
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        ContractEmployee firstAssignment = Assignment(firstAssignmentId, "INT-1", "Interruption 1", firstDate);
        ContractEmployee secondAssignment = Assignment(secondAssignmentId, "INT-2", "Interruption 2", firstDate);
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

    private async Task SeedSingleDayAsync(Guid attendanceTimesheetId, DateTime date, Guid employeeTypeId, Guid? assignmentId, decimal assignmentWorkload = 0.5m)
    {
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Employee employee = await dbContext.Employees.SingleAsync(employee => employee.Id == SeededTestData.JanNovakEmployeeId);
        employee.EmployeeTypeId = employeeTypeId;
        dbContext.EmployeeWorkloads.Add(new EmployeeWorkload { Id = Guid.CreateVersion7(), EmployeeId = SeededTestData.JanNovakEmployeeId, Year = date.Year, Month = date.Month, Workload = 1m });
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
