using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;
using Timesheets.Api.Features.Timesheets;

namespace Timesheets.Api.Features.Attendance;

public sealed record AttendanceFileDetectionResult(string FileName, bool CanImport, bool IsReimport, string? ErrorMessage, string? EmployeePersonalNumber, string? EmployeeName, int? Year, int? Month);

public sealed class AttendanceFileDetector(AttendanceFileReader reader, ILogger<AttendanceFileDetector> logger)
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;
    public const long MaxMultipartBodySizeBytes = MaxFileSizeBytes + 1024 * 1024;

    public string? GetFileValidationError(IFormFile file)
    {
        if (file.Length == 0)
        {
            return "Soubor je prázdný.";
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return "Soubor může mít maximálně 10 MB.";
        }

        return Path.GetExtension(file.FileName).ToLowerInvariant() is ".xls" or ".xlsx" ? null : "Soubor musí být ve formátu .xls nebo .xlsx.";
    }

    public async Task<AttendanceFileDetectionResult> DetectAsync(IFormFile file, Guid employeeId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        EmployeeTarget? employee = await dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => new EmployeeTarget(e.Id, e.PersonalNumber))
            .SingleOrDefaultAsync(cancellationToken);

        return await DetectAsync(file, employee, dbContext, cancellationToken);
    }

    private async Task<AttendanceFileDetectionResult> DetectAsync(
        IFormFile file,
        EmployeeTarget? employee,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        string? fileValidationError = GetFileValidationError(file);
        if (fileValidationError is not null)
        {
            return CreateResult(file, metadata: null, canImport: false, isReimport: false, errorMessage: fileValidationError);
        }

        AttendanceFileMetadata metadata;
        try
        {
            await using Stream stream = file.OpenReadStream();
            metadata = reader.ReadMetadata(stream);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to detect attendance file {FileName}.", file.FileName);
            return CreateResult(file, metadata: null, canImport: false, isReimport: false, errorMessage: "Soubor se nepodařilo přečíst.");
        }

        if (employee is null)
        {
            return CreateResult(file, metadata, canImport: false, isReimport: false, errorMessage: "Zaměstnanec nebyl nalezen.");
        }

        if (metadata.Year <= 0 || metadata.Month is < 1 or > 12)
        {
            return CreateResult(file, metadata, canImport: false, isReimport: false, errorMessage: "Nepodařilo se určit období výkazu.");
        }

        if (string.IsNullOrWhiteSpace(metadata.EmployeePersonalNumber))
        {
            return CreateResult(file, metadata, canImport: false, isReimport: false, errorMessage: "Nepodařilo se určit osobní číslo zaměstnance.");
        }

        if (!employee.PersonalNumber.Contains(metadata.EmployeePersonalNumber.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return CreateResult(file, metadata, canImport: false, isReimport: false, errorMessage: "Soubor nepatří vybranému zaměstnanci.");
        }

        Domain.Models.Timesheet? existingTimesheet = await dbContext.Timesheets
            .AsNoTracking()
            .Include(timesheet => timesheet.TimesheetStatus)
            .FirstOrDefaultAsync(timesheet => timesheet.EmployeeId == employee.Id && timesheet.Year == metadata.Year && timesheet.Month == metadata.Month, cancellationToken);

        if (existingTimesheet is not null)
        {
            if (existingTimesheet.TimesheetStatus.Code != TimesheetStatusCodes.Draft)
            {
                return CreateResult(file, metadata, canImport: false, isReimport: false, errorMessage: "Docházku lze znovu naimportovat jen ve stavu Rozpracovaný.");
            }

            return CreateResult(file, metadata, canImport: true, isReimport: true, errorMessage: null);
        }

        return CreateResult(file, metadata, canImport: true, isReimport: false, errorMessage: null);
    }

    private static AttendanceFileDetectionResult CreateResult(IFormFile file, AttendanceFileMetadata? metadata, bool canImport, bool isReimport, string? errorMessage) =>
        new(file.FileName, canImport, isReimport, errorMessage, metadata?.EmployeePersonalNumber, metadata?.EmployeeName, metadata?.Year, metadata?.Month);

    private sealed record EmployeeTarget(Guid Id, string PersonalNumber);
}
