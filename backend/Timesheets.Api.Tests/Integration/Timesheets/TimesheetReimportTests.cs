using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;
using Timesheets.Api.Timesheets.Endpoints;

namespace Timesheets.Api.Tests.Integration.Timesheets;

public class TimesheetReimportTests : BaseIntegrationTest
{
    public TimesheetReimportTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task DetectTimesheetImport_ForNewMonth_ReturnsCanImportWithoutReimport()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(
            Client,
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            workload: 0.5m);

        byte[] fileBytes = AttendanceTimesheetTestFileBuilder.Create(
            setup.EmployeePersonalNumber,
            "Test Employee",
            2024,
            10,
            50m);

        using MultipartFormDataContent form = TimesheetImportFormFactory.Create(setup.EmployeeId, fileBytes, "attendance.xlsx");
        HttpResponseMessage response = await Client.PostAsync("/api/timesheets/detect", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        DetectTimesheetImport.Response? payload = await response.Content.ReadFromJsonAsync<DetectTimesheetImport.Response>();
        Assert.NotNull(payload);
        Assert.True(payload!.Result.CanImport);
        Assert.False(payload.Result.IsReimport);
    }

    [Fact]
    public async Task DetectTimesheetImport_ForDraftMonth_ReturnsReimport()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(
            Client,
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            workload: 0.5m);

        byte[] fileBytes = AttendanceTimesheetTestFileBuilder.Create(
            setup.EmployeePersonalNumber,
            "Test Employee",
            2024,
            10,
            50m);

        using (MultipartFormDataContent importForm = TimesheetImportFormFactory.Create(setup.EmployeeId, fileBytes, "attendance.xlsx"))
        {
            HttpResponseMessage importResponse = await Client.PostAsync("/api/timesheets/", importForm);
            Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);
        }

        using MultipartFormDataContent detectForm = TimesheetImportFormFactory.Create(setup.EmployeeId, fileBytes, "attendance.xlsx");
        HttpResponseMessage detectResponse = await Client.PostAsync("/api/timesheets/detect", detectForm);

        Assert.Equal(HttpStatusCode.OK, detectResponse.StatusCode);

        DetectTimesheetImport.Response? payload = await detectResponse.Content.ReadFromJsonAsync<DetectTimesheetImport.Response>();
        Assert.NotNull(payload);
        Assert.True(payload!.Result.CanImport);
        Assert.True(payload.Result.IsReimport);
    }

    [Fact]
    public async Task DetectTimesheetImport_ForSubmittedMonth_ReturnsBlocked()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(
            Client,
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            workload: 0.5m);

        byte[] fileBytes = AttendanceTimesheetTestFileBuilder.Create(
            setup.EmployeePersonalNumber,
            "Test Employee",
            2024,
            10,
            50m);

        using (MultipartFormDataContent importForm = TimesheetImportFormFactory.Create(setup.EmployeeId, fileBytes, "attendance.xlsx"))
        {
            HttpResponseMessage importResponse = await Client.PostAsync("/api/timesheets/", importForm);
            Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);
        }

        await SetAttendanceTimesheetStatusAsync(setup.EmployeeId, 2024, 10, TestTimesheetStatusIds.Submitted);

        using MultipartFormDataContent detectForm = TimesheetImportFormFactory.Create(setup.EmployeeId, fileBytes, "attendance.xlsx");
        HttpResponseMessage detectResponse = await Client.PostAsync("/api/timesheets/detect", detectForm);

        Assert.Equal(HttpStatusCode.OK, detectResponse.StatusCode);

        DetectTimesheetImport.Response? payload = await detectResponse.Content.ReadFromJsonAsync<DetectTimesheetImport.Response>();
        Assert.NotNull(payload);
        Assert.False(payload!.Result.CanImport);
        Assert.False(payload.Result.IsReimport);
        Assert.Contains("Rozpracovaný", payload.Result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportTimesheet_Reimport_PreservesTimesheetIdAndComments()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(
            Client,
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            workload: 0.5m);

        byte[] initialFileBytes = AttendanceTimesheetTestFileBuilder.Create(
            setup.EmployeePersonalNumber,
            "Test Employee",
            2024,
            10,
            50m);

        Guid timesheetId;
        using (MultipartFormDataContent importForm = TimesheetImportFormFactory.Create(setup.EmployeeId, initialFileBytes, "attendance.xlsx"))
        {
            HttpResponseMessage importResponse = await Client.PostAsync("/api/timesheets/", importForm);
            Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

            ImportTimesheet.Response? importPayload = await importResponse.Content.ReadFromJsonAsync<ImportTimesheet.Response>();
            Assert.NotNull(importPayload);
            Assert.True(importPayload!.Result.Success);
            Assert.NotNull(importPayload.Result.TimesheetId);
            timesheetId = importPayload.Result.TimesheetId!.Value;
        }

        await AddAttendanceCommentAsync(timesheetId, "Test comment preserved on reimport");

        byte[] updatedFileBytes = AttendanceTimesheetTestFileBuilder.Create(
            setup.EmployeePersonalNumber,
            "Test Employee",
            2024,
            10,
            50m);

        using (MultipartFormDataContent reimportForm = TimesheetImportFormFactory.Create(setup.EmployeeId, updatedFileBytes, "attendance-updated.xlsx"))
        {
            HttpResponseMessage reimportResponse = await Client.PostAsync("/api/timesheets/", reimportForm);
            Assert.Equal(HttpStatusCode.OK, reimportResponse.StatusCode);

            ImportTimesheet.Response? reimportPayload = await reimportResponse.Content.ReadFromJsonAsync<ImportTimesheet.Response>();
            Assert.NotNull(reimportPayload);
            Assert.True(reimportPayload!.Result.Success, reimportPayload.Result.ErrorMessage ?? "Reimport failed without error message.");
            Assert.Equal(timesheetId, reimportPayload.Result.TimesheetId);
        }

        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        bool commentExists = await dbContext.TimesheetComments
            .AsNoTracking()
            .AnyAsync(comment => comment.AttendanceTimesheetId == timesheetId && comment.Text == "Test comment preserved on reimport");

        Assert.True(commentExists);

        int dayCount = await dbContext.AttendanceDays
            .AsNoTracking()
            .CountAsync(day => day.AttendanceTimesheetId == timesheetId);

        Assert.Equal(31, dayCount);
    }

    private async Task SetAttendanceTimesheetStatusAsync(Guid employeeId, int year, int month, Guid statusId)
    {
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        int affected = await dbContext.AttendanceTimesheets
            .Where(timesheet => timesheet.EmployeeId == employeeId && timesheet.Year == year && timesheet.Month == month)
            .ExecuteUpdateAsync(setters => setters.SetProperty(timesheet => timesheet.TimesheetStatusId, statusId));

        Assert.Equal(1, affected);
    }

    private async Task AddAttendanceCommentAsync(Guid attendanceTimesheetId, string text)
    {
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.TimesheetComments.Add(new TimesheetComment
        {
            Id = Guid.NewGuid(),
            AttendanceTimesheetId = attendanceTimesheetId,
            AuthorEmployeeId = SeededTestData.JanNovakEmployeeId,
            Text = text,
        });

        await dbContext.SaveChangesAsync();
    }
}
