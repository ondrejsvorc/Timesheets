using System.Text.Json;
using System.Text.RegularExpressions;
using CzechHolidays;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;
using Timesheets.Api.Domain.Models;

namespace Timesheets.Api.Features.Timesheets;

public sealed record AttendanceTimesheetImportResult(string FileName, bool Success, string? ErrorMessage, Guid? TimesheetId, int? Year, int? Month);
public sealed record AttendanceTimesheetDetectionResult(string FileName, bool CanImport, bool IsReimport, string? ErrorMessage, string? EmployeePersonalNumber, string? EmployeeName, int? Year, int? Month);
internal sealed record AttendanceTimesheetImportTarget(Guid Id, string PersonalNumber);
internal sealed record DetectionAttempt(AttendanceTimesheetMetadata? Metadata, AttendanceTimesheetDetectionResult Result);

public sealed class AttendanceImport(AppDbContext dbContext, ICzechHolidaysFactory holidaysFactory, ILogger<AttendanceImport> logger)
{
    private sealed class AttendanceImportException(string message) : Exception(message);

    internal const long MaxFileSizeBytes = 10 * 1024 * 1024;
    internal const long MaxMultipartBodySizeBytes = MaxFileSizeBytes + 1024 * 1024;

    public async Task<AttendanceTimesheetDetectionResult> DetectAsync(Guid employeeId, IFormFile file, CancellationToken cancellationToken)
    {
        AttendanceTimesheetImportTarget? employee = await GetImportTargetAsync(employeeId, cancellationToken);
        return (await DetectFileAsync(file, employee, cancellationToken)).Result;
    }

    public async Task<AttendanceTimesheetImportResult> ImportAsync(Guid employeeId, IFormFile file, CancellationToken cancellationToken)
    {
        AttendanceTimesheetImportTarget? employee = await GetImportTargetAsync(employeeId, cancellationToken);
        DetectionAttempt detection = await DetectFileAsync(file, employee, cancellationToken);
        if (!detection.Result.CanImport)
        {
            return ToImportResult(detection.Result);
        }

        AttendanceTimesheet timesheet;
        try
        {
            await using Stream stream = file.OpenReadStream();
            timesheet = Read(stream);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read attendance file {FileName}.", file.FileName);
            return new AttendanceTimesheetImportResult(FileName: file.FileName, Success: false, ErrorMessage: "Soubor se nepodařilo přečíst.", TimesheetId: null, Year: detection.Metadata?.Year, Month: detection.Metadata?.Month);
        }

        try
        {
            Guid timesheetId = await PersistAsync(employeeId, timesheet, cancellationToken);
            return new AttendanceTimesheetImportResult(FileName: file.FileName, Success: true, ErrorMessage: null, TimesheetId: timesheetId, Year: timesheet.Year, Month: timesheet.Month);
        }
        catch (AttendanceImportException ex)
        {
            return new AttendanceTimesheetImportResult(FileName: file.FileName, Success: false, ErrorMessage: ex.Message, TimesheetId: null, Year: timesheet.Year, Month: timesheet.Month);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to import attendance file {FileName}.", file.FileName);
            return new AttendanceTimesheetImportResult(FileName: file.FileName, Success: false, ErrorMessage: "Import se nepodařilo dokončit.", TimesheetId: null, Year: timesheet.Year, Month: timesheet.Month);
        }
    }

    private async Task<AttendanceTimesheetImportTarget?> GetImportTargetAsync(Guid employeeId, CancellationToken cancellationToken) =>
        await dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => new AttendanceTimesheetImportTarget(e.Id, e.PersonalNumber))
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<DetectionAttempt> DetectFileAsync(IFormFile file, AttendanceTimesheetImportTarget? employee, CancellationToken cancellationToken)
    {
        string? fileValidationError = GetFileValidationError(file);
        if (fileValidationError is not null)
        {
            return CreateDetection(file, metadata: null, canImport: false, isReimport: false, errorMessage: fileValidationError);
        }

        AttendanceTimesheetMetadata metadata;
        try
        {
            await using Stream stream = file.OpenReadStream();
            metadata = AttendanceSpreadsheet.ReadMetadata(stream);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to detect attendance file {FileName}.", file.FileName);
            return CreateDetection(file, metadata: null, canImport: false, isReimport: false, errorMessage: "Soubor se nepodařilo přečíst.");
        }

        if (employee is null)
        {
            return CreateDetection(file, metadata, canImport: false, isReimport: false, errorMessage: "Zaměstnanec nebyl nalezen.");
        }

        if (metadata.Year <= 0 || metadata.Month is < 1 or > 12)
        {
            return CreateDetection(file, metadata, canImport: false, isReimport: false, errorMessage: "Nepodařilo se určit období výkazu.");
        }

        if (string.IsNullOrWhiteSpace(metadata.EmployeePersonalNumber))
        {
            return CreateDetection(file, metadata, canImport: false, isReimport: false, errorMessage: "Nepodařilo se určit osobní číslo zaměstnance.");
        }

        if (!employee.PersonalNumber.Contains(metadata.EmployeePersonalNumber.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return CreateDetection(file, metadata, canImport: false, isReimport: false, errorMessage: "Soubor nepatří vybranému zaměstnanci.");
        }

        Domain.Models.Timesheet? existingTimesheet = await dbContext.Timesheets
            .AsNoTracking()
            .Include(timesheet => timesheet.TimesheetStatus)
            .FirstOrDefaultAsync(timesheet => timesheet.EmployeeId == employee.Id && timesheet.Year == metadata.Year && timesheet.Month == metadata.Month, cancellationToken);

        if (existingTimesheet is not null)
        {
            if (existingTimesheet.TimesheetStatus.Code != TimesheetStatusCodes.Draft)
            {
                return CreateDetection(file, metadata, canImport: false, isReimport: false, errorMessage: "Docházku lze znovu naimportovat jen ve stavu Rozpracovaný.");
            }

            return CreateDetection(file, metadata, canImport: true, isReimport: true, errorMessage: null);
        }

        return CreateDetection(file, metadata, canImport: true, isReimport: false, errorMessage: null);
    }

    private static DetectionAttempt CreateDetection(IFormFile file, AttendanceTimesheetMetadata? metadata, bool canImport, bool isReimport, string? errorMessage)
    {
        AttendanceTimesheetDetectionResult result = new(FileName: file.FileName, CanImport: canImport, IsReimport: isReimport, ErrorMessage: errorMessage, EmployeePersonalNumber: metadata?.EmployeePersonalNumber, EmployeeName: metadata?.EmployeeName, Year: metadata?.Year, Month: metadata?.Month);
        return new DetectionAttempt(Metadata: metadata, Result: result);
    }

    private static AttendanceTimesheetImportResult ToImportResult(AttendanceTimesheetDetectionResult result) => new(FileName: result.FileName, Success: false, ErrorMessage: result.ErrorMessage, TimesheetId: null, Year: result.Year, Month: result.Month);

    internal static string? GetFileValidationError(IFormFile file)
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

    public AttendanceTimesheet Read(Stream stream)
    {
        AttendanceTimesheet timesheet = AttendanceSpreadsheet.Read(stream);
        HashSet<DateOnly> holidays = holidaysFactory.Create(timesheet.Year).Select(holiday => holiday.Date).ToHashSet();
        return timesheet with { Days = timesheet.Days.Select(day => day with { IsHoliday = holidays.Contains(DateOnly.FromDateTime(day.Date)) }).ToArray() };
    }

    public async Task<Guid> PersistAsync(Guid employeeId, AttendanceTimesheet importedTimesheet, CancellationToken cancellationToken)
    {
        HashSet<string> validInterruptionCodes = await dbContext.Interruptions
            .AsNoTracking()
            .Select(i => i.Name)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken);

        await ContractPartInitializer.EnsureForEmployeeMonthAsync(employeeId, importedTimesheet.Year, importedTimesheet.Month, dbContext, holidaysFactory, cancellationToken);

        decimal projectWorkload = await dbContext.ContractParts
            .AsNoTracking()
            .Where(t => t.Timesheet.EmployeeId == employeeId && t.Timesheet.Year == importedTimesheet.Year && t.Timesheet.Month == importedTimesheet.Month)
            .SumAsync(t => (decimal?)t.Workload, cancellationToken) ?? 0m;

        if (projectWorkload > importedTimesheet.Workload)
        {
            throw new AttendanceImportException($"Nelze importovat. Projektové úvazky pro {importedTimesheet.Month:00}/{importedTimesheet.Year} jsou {projectWorkload:0.##}, ale importovaný celkový úvazek je {importedTimesheet.Workload:0.##}. Nejdřív upravte přiřazení na zakázky.");
        }

        Domain.Models.Timesheet? existingTimesheet = await dbContext.Timesheets
            .Include(timesheet => timesheet.TimesheetStatus)
            .FirstOrDefaultAsync(timesheet => timesheet.EmployeeId == employeeId && timesheet.Year == importedTimesheet.Year && timesheet.Month == importedTimesheet.Month, cancellationToken);

        if (existingTimesheet is not null)
        {
            if (existingTimesheet.TimesheetStatus.Code != TimesheetStatusCodes.Draft)
            {
                throw new AttendanceImportException("Docházku lze znovu naimportovat jen ve stavu Rozpracovaný.");
            }

            return await ReimportAsync(existingTimesheet, employeeId, importedTimesheet, validInterruptionCodes, cancellationToken);
        }

        Domain.Models.TimesheetStatus draftStatus = await dbContext.TimesheetStatuses
            .AsNoTracking()
            .SingleAsync(s => s.Code == TimesheetStatusCodes.Draft, cancellationToken);

        Guid employeeTypeId = await dbContext.Employees
            .AsNoTracking()
            .Where(employee => employee.Id == employeeId)
            .Select(employee => employee.EmployeeTypeId)
            .SingleAsync(cancellationToken);

        Domain.Models.Timesheet timesheet = new()
        {
            Id = Guid.CreateVersion7(),
            EmployeeId = employeeId,
            TimesheetStatusId = draftStatus.Id,
            Year = importedTimesheet.Year,
            Month = importedTimesheet.Month,
            CreatedAt = DateTime.UtcNow
        };

        TimesheetBootstrap.AddMonth(dbContext, timesheet, employeeTypeId);
        AddImportedDays(timesheet.Id, importedTimesheet, validInterruptionCodes);

        await UpsertEmployeeWorkloadAsync(employeeId, importedTimesheet.Year, importedTimesheet.Month, importedTimesheet.Workload, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await TimesheetEngine.ApplyInterruptionHoursAsync(timesheet.Id, dbContext, cancellationToken);
        return timesheet.Id;
    }

    private async Task<Guid> ReimportAsync(Domain.Models.Timesheet existingTimesheet, Guid employeeId, AttendanceTimesheet importedTimesheet, HashSet<string> validInterruptionCodes, CancellationToken cancellationToken)
    {
        Guid timesheetId = existingTimesheet.Id;

        List<Domain.Models.AttendanceDay> existingDays = await dbContext.AttendanceDays
            .Where(day => day.AttendanceId == timesheetId)
            .ToListAsync(cancellationToken);
        dbContext.AttendanceDays.RemoveRange(existingDays);

        AddImportedDays(timesheetId, importedTimesheet, validInterruptionCodes);
        await UpsertEmployeeWorkloadAsync(employeeId, importedTimesheet.Year, importedTimesheet.Month, importedTimesheet.Workload, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Timesheets
            .Where(t => t.Id == timesheetId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.UpdatedAt, DateTime.UtcNow), cancellationToken);

        await RecalculateDraftContractPartColumnsAsync(employeeId, importedTimesheet.Year, importedTimesheet.Month, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await TimesheetEngine.ApplyInterruptionHoursAsync(timesheetId, dbContext, cancellationToken);
        return timesheetId;
    }

    private void AddImportedDays(Guid attendanceId, AttendanceTimesheet importedTimesheet, HashSet<string> validInterruptionCodes)
    {
        foreach (Features.Timesheets.AttendanceDay day in importedTimesheet.Days)
        {
            dbContext.AttendanceDays.Add(new Domain.Models.AttendanceDay
            {
                Id = Guid.CreateVersion7(),
                AttendanceId = attendanceId,
                Date = ToUtcDate(day.Date),
                ClockIn = day.ClockIn,
                ClockOut = day.ClockOut,
                BreakStart = day.BreakStart,
                BreakEnd = day.BreakEnd,
                Workload = day.Workload,
                HoursWithoutBreak = day.TotalHours,
                HoursObligation = day.TotalHoursObligation,
                IsHoliday = day.IsHoliday,
                Description = NormalizeImportedInterruptions(day.OtherInterruption, validInterruptionCodes),
                Schedules = JsonSerializer.Serialize(day.Schedules)
            });
        }
    }

    private async Task RecalculateDraftContractPartColumnsAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken)
    {
        Domain.Models.Attendance? attendance = await dbContext.Attendances
            .AsNoTracking()
            .Include(a => a.Days)
            .Where(a => a.Timesheet.EmployeeId == employeeId && a.Timesheet.Year == year && a.Timesheet.Month == month)
            .SingleOrDefaultAsync(cancellationToken);

        if (attendance is null)
        {
            return;
        }

        Dictionary<DateTime, Domain.Models.AttendanceDay> attendanceByDate = attendance.Days
            .ToDictionary(day => ToUtcDate(day.Date).Date);

        List<Domain.Models.ContractPart> contractParts = await dbContext.ContractParts
            .Include(pt => pt.Days)
            .Where(pt => pt.Timesheet.EmployeeId == employeeId && pt.Timesheet.Year == year && pt.Timesheet.Month == month)
            .Where(pt => pt.TimesheetStatus.Code == TimesheetStatusCodes.Draft)
            .ToListAsync(cancellationToken);

        foreach (Domain.Models.ContractPart projectTimesheet in contractParts)
        {
            foreach (Domain.Models.ContractPartDay contractPartDay in projectTimesheet.Days)
            {
                if (!attendanceByDate.TryGetValue(ToUtcDate(contractPartDay.Date).Date, out Domain.Models.AttendanceDay? attendanceDay))
                {
                    continue;
                }

                contractPartDay.IsHoliday = attendanceDay.IsHoliday;
                contractPartDay.HoursObligation = TimesheetLogic.CalculateTotalHoursObligation(contractPartDay.Date, attendanceDay.IsHoliday, projectTimesheet.Workload);
            }

            projectTimesheet.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task UpsertEmployeeWorkloadAsync(Guid employeeId, int year, int month, decimal workload, CancellationToken cancellationToken)
    {
        Domain.Models.EmployeeWorkload? existing = await dbContext.EmployeeWorkloads
            .FirstOrDefaultAsync(w => w.EmployeeId == employeeId && w.Year == year && w.Month == month, cancellationToken);

        if (existing is null)
        {
            dbContext.EmployeeWorkloads.Add(new Domain.Models.EmployeeWorkload
            {
                Id = Guid.CreateVersion7(),
                EmployeeId = employeeId,
                Year = year,
                Month = month,
                Workload = workload,
            });
            return;
        }

        existing.Workload = workload;
    }

    private static DateTime ToUtcDate(DateTime value) => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static string? NormalizeImportedInterruptions(string? raw, HashSet<string> validCodes)
    {
        if (string.IsNullOrWhiteSpace(raw) || validCodes.Count == 0)
        {
            return null;
        }

        string cleaned = Regex.Replace(raw, @"\([^)]*\)", " ");
        cleaned = cleaned.Replace(";", " ").Replace(",", " ").Replace("|", " ");

        List<string> normalized = [];
        string[] chunks = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (string chunk in chunks)
        {
            string token = Regex.Replace(chunk.ToUpperInvariant(), @"[^A-Z/]", "");
            if (token.Length == 0)
            {
                continue;
            }

            if (TryResolveInterruptionCode(token, validCodes, out string? resolved) && resolved is not null)
            {
                if (!normalized.Contains(resolved, StringComparer.OrdinalIgnoreCase))
                {
                    normalized.Add(resolved);
                }
            }
        }

        return normalized.Count == 0 ? null : string.Join(",", normalized);
    }

    private static bool TryResolveInterruptionCode(string token, HashSet<string> validCodes, out string? resolved)
    {
        if (validCodes.Contains(token))
        {
            resolved = token;
            return true;
        }

        string alpha = Regex.Replace(token, @"[^A-Z/]", "");
        if (alpha.Length > 0 && validCodes.Contains(alpha))
        {
            resolved = alpha;
            return true;
        }

        string? prefixMatch = validCodes
            .Where(code => alpha.StartsWith(code, StringComparison.OrdinalIgnoreCase) || code.StartsWith(alpha, StringComparison.OrdinalIgnoreCase))
            .OrderBy(code => Math.Abs(code.Length - alpha.Length))
            .ThenBy(code => code.Length)
            .FirstOrDefault();

        resolved = prefixMatch;
        return resolved is not null;
    }
}
