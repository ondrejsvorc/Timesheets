using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Domain;
using Timesheets.Api.Domain.Models;
using Timesheets.Api.Features.Attendance;
using Timesheets.Api.Features.Attendance.Endpoints;
using Timesheets.Api.Features.Employees;
using Timesheets.Api.Features.Timesheets;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Timesheets;

public class TimesheetReimportTests : BaseIntegrationTest
{
    public TimesheetReimportTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task DetectAttendance_WithEmptyFile_ReturnsBadRequest()
    {
        using MultipartFormDataContent form = TimesheetImportFormFactory.Create(SeededTestData.JanNovakEmployeeId, [], "attendance.xlsx");
        HttpResponseMessage response = await Client.PostAsync("/api/attendance/detect", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DetectAttendance_WithOversizedFile_ReturnsBadRequest()
    {
        using MultipartFormDataContent form = TimesheetImportFormFactory.Create(SeededTestData.JanNovakEmployeeId, new byte[10 * 1024 * 1024 + 1], "attendance.xlsx");
        HttpResponseMessage response = await Client.PostAsync("/api/attendance/detect", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DetectAttendance_WithMalformedFile_ReturnsGenericError()
    {
        using MultipartFormDataContent form = TimesheetImportFormFactory.Create(SeededTestData.JanNovakEmployeeId, [1, 2, 3], "attendance.xlsx");
        HttpResponseMessage response = await Client.PostAsync("/api/attendance/detect", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        AttendanceFileDetectionResult? payload = await response.Content.ReadFromJsonAsync<AttendanceFileDetectionResult>();
        Assert.Equal("Soubor se nepodařilo přečíst.", payload!.ErrorMessage);
    }

    [Fact]
    public async Task DetectAttendance_ForNewMonth_ReturnsCanImportWithoutReimport()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), workload: 0.5m);
        byte[] fileBytes = AttendanceTimesheetTestFileBuilder.Create(setup.EmployeePersonalNumber, "Test Employee", 2024, 10, 50m);
        using MultipartFormDataContent form = TimesheetImportFormFactory.Create(setup.EmployeeId, fileBytes, "attendance.xlsx");
        HttpResponseMessage response = await Client.PostAsync("/api/attendance/detect", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        AttendanceFileDetectionResult? payload = await response.Content.ReadFromJsonAsync<AttendanceFileDetectionResult>();
        Assert.NotNull(payload);
        Assert.True(payload!.CanImport);
        Assert.False(payload.IsReimport);
    }

    [Fact]
    public async Task DetectAttendance_ForDraftMonth_ReturnsReimport()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), workload: 0.5m);
        byte[] fileBytes = AttendanceTimesheetTestFileBuilder.Create(setup.EmployeePersonalNumber, "Test Employee", 2024, 10, 50m);

        using (MultipartFormDataContent importForm = TimesheetImportFormFactory.Create(setup.EmployeeId, fileBytes, "attendance.xlsx"))
        {
            HttpResponseMessage importResponse = await Client.PostAsync("/api/attendance", importForm);
            Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);
        }

        using MultipartFormDataContent detectForm = TimesheetImportFormFactory.Create(setup.EmployeeId, fileBytes, "attendance.xlsx");
        HttpResponseMessage detectResponse = await Client.PostAsync("/api/attendance/detect", detectForm);
        Assert.Equal(HttpStatusCode.OK, detectResponse.StatusCode);

        AttendanceFileDetectionResult? payload = await detectResponse.Content.ReadFromJsonAsync<AttendanceFileDetectionResult>();
        Assert.NotNull(payload);
        Assert.True(payload!.CanImport);
        Assert.True(payload.IsReimport);
    }

    [Fact]
    public async Task ImportAttendance_Reimport_UsesAttendanceIdWhenItDiffersFromTimesheetId()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), workload: 0.5m);
        Guid timesheetId = Guid.CreateVersion7();
        Guid attendanceId = Guid.CreateVersion7();

        using (IServiceScope scope = CreateScope())
        {
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Timesheets.Add(new Timesheet { Id = timesheetId, EmployeeId = setup.EmployeeId, TimesheetStatusId = TestTimesheetStatusIds.Draft, Year = 2024, Month = 10 });
            dbContext.Attendances.Add(new global::Timesheets.Api.Domain.Models.Attendance { Id = attendanceId, TimesheetId = timesheetId, EmployeeTypeId = EmployeeTypes.NonAcademicId });
            await dbContext.SaveChangesAsync();
        }

        byte[] fileBytes = AttendanceTimesheetTestFileBuilder.Create(setup.EmployeePersonalNumber, "Test Employee", 2024, 10, 50m);
        using MultipartFormDataContent form = TimesheetImportFormFactory.Create(setup.EmployeeId, fileBytes, "attendance.xlsx");
        HttpResponseMessage response = await Client.PostAsync("/api/attendance", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using IServiceScope assertionScope = CreateScope();
        AppDbContext assertionContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(31, await assertionContext.AttendanceDays.AsNoTracking().CountAsync(day => day.AttendanceId == attendanceId));
        Assert.Equal(0, await assertionContext.AttendanceDays.AsNoTracking().CountAsync(day => day.AttendanceId == timesheetId));
    }

    [Fact]
    public async Task ImportAttendance_Reimport_CreatesMissingAttendanceForDraftTimesheet()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), workload: 0.5m);
        await SetEmployeeTypeAsync(setup.EmployeeId, EmployeeTypes.NonAcademicId);
        Guid timesheetId = Guid.CreateVersion7();

        using (IServiceScope scope = CreateScope())
        {
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Timesheets.Add(new Timesheet { Id = timesheetId, EmployeeId = setup.EmployeeId, TimesheetStatusId = TestTimesheetStatusIds.Draft, Year = 2024, Month = 10 });
            await dbContext.SaveChangesAsync();
        }

        byte[] fileBytes = AttendanceTimesheetTestFileBuilder.Create(setup.EmployeePersonalNumber, "Test Employee", 2024, 10, 50m);
        using MultipartFormDataContent form = TimesheetImportFormFactory.Create(setup.EmployeeId, fileBytes, "attendance.xlsx");
        HttpResponseMessage response = await Client.PostAsync("/api/attendance", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ImportAttendance.Response? payload = await response.Content.ReadFromJsonAsync<ImportAttendance.Response>();
        Assert.NotNull(payload);
        Assert.Equal(timesheetId, payload!.TimesheetId);

        using IServiceScope assertionScope = CreateScope();
        AppDbContext assertionContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        global::Timesheets.Api.Domain.Models.Attendance attendance = await assertionContext.Attendances
            .AsNoTracking()
            .SingleAsync(attendance => attendance.TimesheetId == timesheetId);
        Assert.Equal(EmployeeTypes.NonAcademicId, attendance.EmployeeTypeId);
        Assert.Equal(31, await assertionContext.AttendanceDays.AsNoTracking().CountAsync(day => day.AttendanceId == attendance.Id));
    }

    [Fact]
    public async Task DetectAttendance_ForSubmittedMonth_ReturnsBlocked()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), workload: 0.5m);
        byte[] fileBytes = AttendanceTimesheetTestFileBuilder.Create(setup.EmployeePersonalNumber, "Test Employee", 2024, 10, 50m);

        using (MultipartFormDataContent importForm = TimesheetImportFormFactory.Create(setup.EmployeeId, fileBytes, "attendance.xlsx"))
        {
            HttpResponseMessage importResponse = await Client.PostAsync("/api/attendance", importForm);
            Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);
        }

        await SetTimesheetStatusAsync(setup.EmployeeId, 2024, 10, TestTimesheetStatusIds.Submitted);

        using MultipartFormDataContent detectForm = TimesheetImportFormFactory.Create(setup.EmployeeId, fileBytes, "attendance.xlsx");
        HttpResponseMessage detectResponse = await Client.PostAsync("/api/attendance/detect", detectForm);
        Assert.Equal(HttpStatusCode.OK, detectResponse.StatusCode);

        AttendanceFileDetectionResult? payload = await detectResponse.Content.ReadFromJsonAsync<AttendanceFileDetectionResult>();
        Assert.NotNull(payload);
        Assert.False(payload!.CanImport);
        Assert.False(payload.IsReimport);
        Assert.Contains("Rozpracovaný", payload.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportAttendance_SnapshotsCurrentEmployeeType()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), workload: 0.5m);
        await SetEmployeeTypeAsync(setup.EmployeeId, EmployeeTypes.NonAcademicId);
        byte[] fileBytes = AttendanceTimesheetTestFileBuilder.Create(setup.EmployeePersonalNumber, "Test Employee", 2024, 10, 50m);

        using MultipartFormDataContent importForm = TimesheetImportFormFactory.Create(setup.EmployeeId, fileBytes, "attendance.xlsx");
        HttpResponseMessage importResponse = await Client.PostAsync("/api/attendance", importForm);

        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);
        ImportAttendance.Response? importPayload = await importResponse.Content.ReadFromJsonAsync<ImportAttendance.Response>();
        Assert.NotNull(importPayload);

        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Guid? employeeTypeId = await dbContext.Attendances
            .AsNoTracking()
            .Where(attendance => attendance.Timesheet.EmployeeId == setup.EmployeeId && attendance.Timesheet.Year == 2024 && attendance.Timesheet.Month == 10)
            .Select(attendance => attendance.EmployeeTypeId)
            .SingleAsync();

        Assert.Equal(EmployeeTypes.NonAcademicId, employeeTypeId);
    }

    [Fact]
    public async Task ImportAttendance_ClearsAttendanceTimesForTwoHalfDayInterruptions()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), workload: 0.5m);
        await SetEmployeeTypeAsync(setup.EmployeeId, EmployeeTypes.NonAcademicId);
        byte[] fileBytes = AttendanceTimesheetTestFileBuilder.Create(
            setup.EmployeePersonalNumber,
            "Test Employee",
            2026,
            1,
            workloadPercent: 100m,
            configure: sheet =>
            {
                const int row = 33;
                sheet.Cell($"B{row}").Value = "00:00";
                sheet.Cell($"C{row}").Value = "00:00";
                sheet.Cell($"D{row}").Value = "00:00";
                sheet.Cell($"E{row}").Value = "00:00";
                sheet.Cell($"F{row}").Value = "D p\u016flden, JMV/HO p\u016flden";
            });

        using MultipartFormDataContent form = TimesheetImportFormFactory.Create(setup.EmployeeId, fileBytes, "attendance.xlsx");
        HttpResponseMessage importResponse = await Client.PostAsync("/api/attendance", form);

        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        DateTime date = new(2026, 1, 30, 0, 0, 0, DateTimeKind.Utc);
        global::Timesheets.Api.Domain.Models.AttendanceDay day = await dbContext.AttendanceDays
            .AsNoTracking()
            .SingleAsync(day => day.Attendance.Timesheet.EmployeeId == setup.EmployeeId && day.Date == date);
        Assert.Null(day.ClockIn);
        Assert.Null(day.ClockOut);
        Assert.Null(day.BreakStart);
        Assert.Null(day.BreakEnd);
        Assert.True(TimesheetEvaluator.HasFullDayInterruption(day.Description));
    }

    [Fact]
    public async Task ImportAttendance_Reimport_PreservesTimesheetIdAndComments()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), workload: 0.5m);
        byte[] initialFileBytes = AttendanceTimesheetTestFileBuilder.Create(setup.EmployeePersonalNumber, "Test Employee", 2024, 10, 50m);
        Guid timesheetId;

        using (MultipartFormDataContent importForm = TimesheetImportFormFactory.Create(setup.EmployeeId, initialFileBytes, "attendance.xlsx"))
        {
            HttpResponseMessage importResponse = await Client.PostAsync("/api/attendance", importForm);
            Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);
            ImportAttendance.Response? importPayload = await importResponse.Content.ReadFromJsonAsync<ImportAttendance.Response>();
            Assert.NotNull(importPayload);
            timesheetId = importPayload!.TimesheetId;
        }

        await AddAttendanceCommentAsync(timesheetId, "Test comment preserved on reimport");

        byte[] updatedFileBytes = AttendanceTimesheetTestFileBuilder.Create(setup.EmployeePersonalNumber, "Test Employee", 2024, 10, 50m);
        using (MultipartFormDataContent reimportForm = TimesheetImportFormFactory.Create(setup.EmployeeId, updatedFileBytes, "attendance-updated.xlsx"))
        {
            HttpResponseMessage reimportResponse = await Client.PostAsync("/api/attendance", reimportForm);
            Assert.Equal(HttpStatusCode.OK, reimportResponse.StatusCode);
            ImportAttendance.Response? reimportPayload = await reimportResponse.Content.ReadFromJsonAsync<ImportAttendance.Response>();
            Assert.NotNull(reimportPayload);
            Assert.Equal(timesheetId, reimportPayload!.TimesheetId);
        }

        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        bool commentExists = await dbContext.TimesheetComments.AsNoTracking().AnyAsync(comment => comment.TimesheetId == timesheetId && comment.Text == "Test comment preserved on reimport");
        Guid attendanceId = await dbContext.Attendances.AsNoTracking().Where(attendance => attendance.TimesheetId == timesheetId).Select(attendance => attendance.Id).SingleAsync();
        int dayCount = await dbContext.AttendanceDays.AsNoTracking().CountAsync(day => day.AttendanceId == attendanceId);
        Assert.True(commentExists);
        Assert.Equal(31, dayCount);
    }

    private async Task SetTimesheetStatusAsync(Guid employeeId, int year, int month, Guid statusId)
    {
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        int affected = await dbContext.Timesheets.Where(timesheet => timesheet.EmployeeId == employeeId && timesheet.Year == year && timesheet.Month == month).ExecuteUpdateAsync(setters => setters.SetProperty(timesheet => timesheet.TimesheetStatusId, statusId));
        Assert.Equal(1, affected);
    }

    private async Task SetEmployeeTypeAsync(Guid employeeId, Guid employeeTypeId)
    {
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        int affected = await dbContext.Employees.Where(employee => employee.Id == employeeId).ExecuteUpdateAsync(setters => setters.SetProperty(employee => employee.EmployeeTypeId, employeeTypeId));
        Assert.Equal(1, affected);
    }

    private async Task AddAttendanceCommentAsync(Guid timesheetId, string text)
    {
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.TimesheetComments.Add(new TimesheetComment { Id = Guid.CreateVersion7(), TimesheetId = timesheetId, AuthorEmployeeId = SeededTestData.JanNovakEmployeeId, Text = text });
        await dbContext.SaveChangesAsync();
    }
}
