using System.Text.Json;
using System.Text.RegularExpressions;
using CzechHolidays;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Timesheets;

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

        // Accept variants like "ST101972" vs "101972" (e.g. "ST101972".Contains("101972")).
        if (!employee.PersonalNumber.Contains(metadata.EmployeePersonalNumber.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return CreateDetection(file, metadata, canImport: false, isReimport: false, errorMessage: "Soubor nepatří vybranému zaměstnanci.");
        }

        Data.Models.AttendanceTimesheet? existingTimesheet = await dbContext.AttendanceTimesheets
            .AsNoTracking()
            .FirstOrDefaultAsync(timesheet => timesheet.EmployeeId == employee.Id && timesheet.Year == metadata.Year && timesheet.Month == metadata.Month, cancellationToken);

        if (existingTimesheet is not null)
        {
            if (existingTimesheet.TimesheetStatusId != TimesheetWorkflow.DraftStatusId)
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

        await ProjectTimesheetInitializer.EnsureForEmployeeMonthAsync(employeeId, importedTimesheet.Year, importedTimesheet.Month, dbContext, holidaysFactory, cancellationToken);

        decimal projectWorkload = await dbContext.ProjectTimesheets
            .AsNoTracking()
            .Where(t => t.EmployeeId == employeeId && t.Year == importedTimesheet.Year && t.Month == importedTimesheet.Month)
            .SumAsync(t => (decimal?)t.Workload, cancellationToken) ?? 0m;

        if (projectWorkload > importedTimesheet.Workload)
        {
            throw new AttendanceImportException($"Nelze importovat. Projektové úvazky pro {importedTimesheet.Month:00}/{importedTimesheet.Year} jsou {projectWorkload:0.##}, ale importovaný celkový úvazek je {importedTimesheet.Workload:0.##}. Nejdřív upravte přiřazení na zakázky.");
        }

        Data.Models.AttendanceTimesheet? existingTimesheet = await dbContext.AttendanceTimesheets
            .FirstOrDefaultAsync(timesheet => timesheet.EmployeeId == employeeId && timesheet.Year == importedTimesheet.Year && timesheet.Month == importedTimesheet.Month, cancellationToken);

        if (existingTimesheet is not null)
        {
            if (existingTimesheet.TimesheetStatusId != TimesheetWorkflow.DraftStatusId)
            {
                throw new AttendanceImportException("Docházku lze znovu naimportovat jen ve stavu Rozpracovaný.");
            }

            return await ReimportAsync(existingTimesheet, employeeId, importedTimesheet, validInterruptionCodes, cancellationToken);
        }

        Data.Models.TimesheetStatus draftStatus = await dbContext.TimesheetStatuses
            .AsNoTracking()
            .SingleAsync(s => s.Name == "Rozpracovaný", cancellationToken);

        Guid? employeeTypeId = await dbContext.Employees
            .AsNoTracking()
            .Where(employee => employee.Id == employeeId)
            .Select(employee => employee.EmployeeTypeId)
            .SingleOrDefaultAsync(cancellationToken);

        Data.Models.AttendanceTimesheet timesheet = new()
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            TimesheetStatusId = draftStatus.Id,
            Year = importedTimesheet.Year,
            Month = importedTimesheet.Month,
            CreatedAt = DateTime.UtcNow
        };

        AddImportedDays(timesheet.Id, importedTimesheet, validInterruptionCodes);
        dbContext.AttendanceTimesheets.Add(timesheet);

        await UpsertEmployeeWorkloadAsync(employeeId, importedTimesheet.Year, importedTimesheet.Month, importedTimesheet.Workload, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await TimesheetEngine.ApplyInterruptionHoursAsync(timesheet.Id, dbContext, cancellationToken);
        return timesheet.Id;
    }

    private async Task<Guid> ReimportAsync(Data.Models.AttendanceTimesheet existingTimesheet, Guid employeeId, AttendanceTimesheet importedTimesheet, HashSet<string> validInterruptionCodes, CancellationToken cancellationToken)
    {
        Guid timesheetId = existingTimesheet.Id;

        List<Data.Models.AttendanceDay> existingDays = await dbContext.AttendanceDays
            .Where(day => day.AttendanceTimesheetId == timesheetId)
            .ToListAsync(cancellationToken);
        dbContext.AttendanceDays.RemoveRange(existingDays);

        AddImportedDays(timesheetId, importedTimesheet, validInterruptionCodes);
        await UpsertEmployeeWorkloadAsync(employeeId, importedTimesheet.Year, importedTimesheet.Month, importedTimesheet.Workload, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.AttendanceTimesheets
            .Where(t => t.Id == timesheetId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.UpdatedAt, DateTime.UtcNow), cancellationToken);

        await RecalculateDraftProjectColumnsAsync(employeeId, importedTimesheet.Year, importedTimesheet.Month, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await TimesheetEngine.ApplyInterruptionHoursAsync(timesheetId, dbContext, cancellationToken);
        return timesheetId;
    }

    private void AddImportedDays(Guid attendanceTimesheetId, AttendanceTimesheet importedTimesheet, HashSet<string> validInterruptionCodes)
    {
        foreach (AttendanceDay day in importedTimesheet.Days)
        {
            dbContext.AttendanceDays.Add(new Data.Models.AttendanceDay
            {
                Id = Guid.NewGuid(),
                AttendanceTimesheetId = attendanceTimesheetId,
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

    private async Task RecalculateDraftProjectColumnsAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken)
    {
        Data.Models.AttendanceTimesheet? attendanceTimesheet = await dbContext.AttendanceTimesheets
            .AsNoTracking()
            .Include(t => t.Days)
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.Year == year && t.Month == month, cancellationToken);

        if (attendanceTimesheet is null)
        {
            return;
        }

        Dictionary<DateTime, Data.Models.AttendanceDay> attendanceByDate = attendanceTimesheet.Days
            .ToDictionary(day => ToUtcDate(day.Date).Date);

        List<Data.Models.ProjectTimesheet> projectTimesheets = await dbContext.ProjectTimesheets
            .Include(pt => pt.Days)
            .Where(pt => pt.EmployeeId == employeeId && pt.Year == year && pt.Month == month)
            .Where(pt => pt.TimesheetStatusId == TimesheetWorkflow.DraftStatusId)
            .ToListAsync(cancellationToken);

        foreach (Data.Models.ProjectTimesheet projectTimesheet in projectTimesheets)
        {
            foreach (Data.Models.ProjectDay projectDay in projectTimesheet.Days)
            {
                if (!attendanceByDate.TryGetValue(ToUtcDate(projectDay.Date).Date, out Data.Models.AttendanceDay? attendanceDay))
                {
                    continue;
                }

                projectDay.IsHoliday = attendanceDay.IsHoliday;
                projectDay.HoursObligation = TimesheetLogic.CalculateTotalHoursObligation(projectDay.Date, attendanceDay.IsHoliday, projectTimesheet.Workload);
            }

            projectTimesheet.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task UpsertEmployeeWorkloadAsync(Guid employeeId, int year, int month, decimal workload, CancellationToken cancellationToken)
    {
        Data.Models.EmployeeWorkload? existing = await dbContext.EmployeeWorkloads
            .FirstOrDefaultAsync(w => w.EmployeeId == employeeId && w.Year == year && w.Month == month, cancellationToken);

        if (existing is null)
        {
            dbContext.EmployeeWorkloads.Add(new Data.Models.EmployeeWorkload
            {
                Id = Guid.NewGuid(),
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

        // Remove parenthetical noise like "(0)" and normalize separators.
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

        // "SCT0" -> "SCT", "NL123" -> "NL"
        string alpha = Regex.Replace(token, @"[^A-Z/]", "");
        if (alpha.Length > 0 && validCodes.Contains(alpha))
        {
            resolved = alpha;
            return true;
        }

        // Best-effort fallback: choose closest prefix match from known DB codes.
        string? prefixMatch = validCodes
            .Where(code => alpha.StartsWith(code, StringComparison.OrdinalIgnoreCase) || code.StartsWith(alpha, StringComparison.OrdinalIgnoreCase))
            .OrderBy(code => Math.Abs(code.Length - alpha.Length))
            .ThenBy(code => code.Length)
            .FirstOrDefault();

        resolved = prefixMatch;
        return resolved is not null;
    }
}
