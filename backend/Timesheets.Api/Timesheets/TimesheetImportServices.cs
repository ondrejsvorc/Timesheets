using System.Text.Json;
using System.Text.RegularExpressions;
using CzechHolidays;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Timesheets;

public sealed record AttendanceTimesheetImportResult(
    string FileName,
    bool Success,
    string? ErrorMessage,
    Guid? TimesheetId,
    int? Year,
    int? Month
);

public sealed record AttendanceTimesheetDetectionResult(
    string FileName,
    bool CanImport,
    bool IsReimport,
    string? ErrorMessage,
    string? EmployeePersonalNumber,
    string? EmployeeName,
    int? Year,
    int? Month
);

public interface IAttendanceTimesheetImportService
{
    Task<AttendanceTimesheetDetectionResult> DetectAsync(Guid employeeId, IFormFile file, CancellationToken cancellationToken);
    Task<AttendanceTimesheetImportResult> ImportAsync(Guid employeeId, IFormFile file, CancellationToken cancellationToken);
}

public interface IAttendanceTimesheetPersistenceService
{
    Task<Guid> PersistAsync(Guid employeeId, AttendanceTimesheet timesheet, CancellationToken cancellationToken);
}

internal sealed record PendingAttendanceTimesheetImport(int Index, IFormFile File, AttendanceTimesheetMetadata Metadata);
internal sealed record AttendanceTimesheetImportTarget(Guid Id, string PersonalNumber);
internal sealed record DetectionAttempt(AttendanceTimesheetMetadata? Metadata, AttendanceTimesheetDetectionResult Result);

public sealed class AttendanceTimesheetImportService(
    AppDbContext dbContext,
    IAttendanceTimesheetMetadataReader metadataReader,
    ITimesheetImporter<AttendanceTimesheet> importer,
    IAttendanceTimesheetPersistenceService persistenceService
) : IAttendanceTimesheetImportService
{
    public async Task<AttendanceTimesheetDetectionResult> DetectAsync(Guid employeeId, IFormFile file, CancellationToken cancellationToken)
    {
        AttendanceTimesheetImportTarget? employee = await GetImportTargetAsync(employeeId, cancellationToken);
        return (await DetectFileAsync(file, employee, cancellationToken)).Result;
    }

    public async Task<AttendanceTimesheetImportResult> ImportAsync(Guid employeeId, IFormFile file, CancellationToken cancellationToken)
    {
        IReadOnlyList<AttendanceTimesheetImportResult> results = await ImportManyAsync(employeeId, [file], cancellationToken);
        return results.Single();
    }

    private async Task<IReadOnlyList<AttendanceTimesheetImportResult>> ImportManyAsync(Guid employeeId, IReadOnlyList<IFormFile> files, CancellationToken cancellationToken)
    {
        if (files.Count == 0)
        {
            return [];
        }

        AttendanceTimesheetImportTarget? employee = await GetImportTargetAsync(employeeId, cancellationToken);

        if (employee is null)
        {
            return files
                .Select(file => new AttendanceTimesheetImportResult(file.FileName, false, "Zaměstnanec nebyl nalezen.", null, null, null))
                .ToArray();
        }

        AttendanceTimesheetImportResult?[] results = new AttendanceTimesheetImportResult?[files.Count];
        List<PendingAttendanceTimesheetImport> pendingImports = [];

        for (int i = 0; i < files.Count; i++)
        {
            IFormFile file = files[i];
            DetectionAttempt detection = await DetectFileAsync(file, employee, cancellationToken);

            if (!detection.Result.CanImport)
            {
                results[i] = ToImportResult(detection.Result);
                continue;
            }

            pendingImports.Add(new PendingAttendanceTimesheetImport(i, file, detection.Metadata!));
        }

        foreach (IGrouping<(int Year, int Month), PendingAttendanceTimesheetImport> duplicateGroup in pendingImports.GroupBy(item => (item.Metadata.Year, item.Metadata.Month)).Where(group => group.Count() > 1))
        {
            foreach (PendingAttendanceTimesheetImport duplicate in duplicateGroup)
            {
                results[duplicate.Index] = new AttendanceTimesheetImportResult(
                    duplicate.File.FileName,
                    false,
                    "V importu je více souborů pro stejné období. Nebyl importován žádný z nich.",
                    null,
                    duplicate.Metadata.Year,
                    duplicate.Metadata.Month
                );
            }
        }

        foreach (PendingAttendanceTimesheetImport pendingImport in pendingImports.Where(item => results[item.Index] is null).OrderBy(item => item.Index))
        {
            try
            {
                await using Stream stream = pendingImport.File.OpenReadStream();
                AttendanceTimesheet importedTimesheet = await importer.ImportAsync(stream);
                Guid timesheetId = await persistenceService.PersistAsync(employeeId, importedTimesheet, cancellationToken);
                results[pendingImport.Index] = new AttendanceTimesheetImportResult(
                    pendingImport.File.FileName,
                    true,
                    null,
                    timesheetId,
                    importedTimesheet.Year,
                    importedTimesheet.Month
                );
            }
            catch (Exception ex)
            {
                results[pendingImport.Index] = new AttendanceTimesheetImportResult(
                    pendingImport.File.FileName,
                    false,
                    $"Chyba při importu: {ex.Message}",
                    null,
                    pendingImport.Metadata.Year,
                    pendingImport.Metadata.Month
                );
            }
        }

        return results.Select(result => result!).ToArray();
    }

    private async Task<AttendanceTimesheetImportTarget?> GetImportTargetAsync(Guid employeeId, CancellationToken cancellationToken) =>
        await dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => new AttendanceTimesheetImportTarget(e.Id, e.PersonalNumber))
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<DetectionAttempt> DetectFileAsync(IFormFile file, AttendanceTimesheetImportTarget? employee, CancellationToken cancellationToken)
    {
        if (!HasSupportedExtension(file.FileName))
        {
            return new DetectionAttempt(
                null,
                new AttendanceTimesheetDetectionResult(file.FileName, false, false, "Soubor musí být ve formátu .xls nebo .xlsx.", null, null, null, null)
            );
        }

        AttendanceTimesheetMetadata metadata;
        try
        {
            await using Stream stream = file.OpenReadStream();
            metadata = metadataReader.Read(stream);
        }
        catch (Exception ex)
        {
            return new DetectionAttempt(
                null,
                new AttendanceTimesheetDetectionResult(file.FileName, false, false, $"Chyba při čtení souboru: {ex.Message}", null, null, null, null)
            );
        }

        if (employee is null)
        {
            return new DetectionAttempt(
                metadata,
                new AttendanceTimesheetDetectionResult(file.FileName, false, false, "Zaměstnanec nebyl nalezen.", metadata.EmployeePersonalNumber, metadata.EmployeeName, metadata.Year, metadata.Month)
            );
        }

        if (metadata.Year <= 0 || metadata.Month is < 1 or > 12)
        {
            return new DetectionAttempt(
                metadata,
                new AttendanceTimesheetDetectionResult(file.FileName, false, false, "Nepodařilo se určit období výkazu.", metadata.EmployeePersonalNumber, metadata.EmployeeName, metadata.Year, metadata.Month)
            );
        }

        if (string.IsNullOrWhiteSpace(metadata.EmployeePersonalNumber))
        {
            return new DetectionAttempt(
                metadata,
                new AttendanceTimesheetDetectionResult(file.FileName, false, false, "Nepodařilo se určit osobní číslo zaměstnance.", metadata.EmployeePersonalNumber, metadata.EmployeeName, metadata.Year, metadata.Month)
            );
        }

        // Accept variants like "ST101972" vs "101972" (e.g. "ST101972".Contains("101972")).
        if (!employee.PersonalNumber.Contains(metadata.EmployeePersonalNumber.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return new DetectionAttempt(
                metadata,
                new AttendanceTimesheetDetectionResult(file.FileName, false, false, "Soubor nepatří vybranému zaměstnanci.", metadata.EmployeePersonalNumber, metadata.EmployeeName, metadata.Year, metadata.Month)
            );
        }

        Data.Models.AttendanceTimesheet? existingTimesheet = await dbContext.AttendanceTimesheets
            .AsNoTracking()
            .FirstOrDefaultAsync(
                timesheet => timesheet.EmployeeId == employee.Id
                    && timesheet.Year == metadata.Year
                    && timesheet.Month == metadata.Month,
                cancellationToken);

        if (existingTimesheet is not null)
        {
            if (existingTimesheet.TimesheetStatusId != TimesheetWorkflowConstants.DraftStatusId)
            {
                return new DetectionAttempt(
                    metadata,
                    new AttendanceTimesheetDetectionResult(
                        file.FileName,
                        false,
                        false,
                        "Docházku lze znovu naimportovat jen ve stavu Rozpracovaný.",
                        metadata.EmployeePersonalNumber,
                        metadata.EmployeeName,
                        metadata.Year,
                        metadata.Month));
            }

            return new DetectionAttempt(
                metadata,
                new AttendanceTimesheetDetectionResult(
                    file.FileName,
                    true,
                    true,
                    null,
                    metadata.EmployeePersonalNumber,
                    metadata.EmployeeName,
                    metadata.Year,
                    metadata.Month));
        }

        return new DetectionAttempt(
            metadata,
            new AttendanceTimesheetDetectionResult(
                file.FileName,
                true,
                false,
                null,
                metadata.EmployeePersonalNumber,
                metadata.EmployeeName,
                metadata.Year,
                metadata.Month));
    }

    private static AttendanceTimesheetImportResult ToImportResult(AttendanceTimesheetDetectionResult result) => new(result.FileName, false, result.ErrorMessage, null, result.Year, result.Month);

    private static bool HasSupportedExtension(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() is ".xls" or ".xlsx";
}

public sealed class AttendanceTimesheetPersistenceService(AppDbContext dbContext, ICzechHolidaysFactory holidaysFactory) : IAttendanceTimesheetPersistenceService
{
    // Matches seeded contract "Kontrakt Alpha-1", where Jan Novak is also assigned as manager.
    private static readonly Guid DefaultImportContractId = Guid.Parse("30000000-0000-0000-0000-000000000001");

    public async Task<Guid> PersistAsync(Guid employeeId, AttendanceTimesheet importedTimesheet, CancellationToken cancellationToken)
    {
        HashSet<string> validInterruptionCodes = await dbContext.Interruptions
            .AsNoTracking()
            .Select(i => i.Name)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken);

        await ProjectTimesheetProvisioner.EnsureForEmployeeMonthAsync(
            employeeId,
            importedTimesheet.Year,
            importedTimesheet.Month,
            dbContext,
            holidaysFactory,
            cancellationToken);

        decimal projectWorkload = await dbContext.ProjectTimesheets
            .AsNoTracking()
            .Where(t => t.EmployeeId == employeeId && t.Year == importedTimesheet.Year && t.Month == importedTimesheet.Month)
            .SumAsync(t => (decimal?)t.Workload, cancellationToken) ?? 0m;

        if (projectWorkload > importedTimesheet.Workload)
        {
            throw new InvalidOperationException(
                $"Nelze importovat. Projektové úvazky pro {importedTimesheet.Month:00}/{importedTimesheet.Year} jsou {projectWorkload:0.##}, ale importovaný celkový úvazek je {importedTimesheet.Workload:0.##}. Nejdřív upravte přiřazení na zakázky."
            );
        }

        Data.Models.AttendanceTimesheet? existingTimesheet = await dbContext.AttendanceTimesheets
            .FirstOrDefaultAsync(
                timesheet => timesheet.EmployeeId == employeeId
                    && timesheet.Year == importedTimesheet.Year
                    && timesheet.Month == importedTimesheet.Month,
                cancellationToken);

        if (existingTimesheet is not null)
        {
            if (existingTimesheet.TimesheetStatusId != TimesheetWorkflowConstants.DraftStatusId)
            {
                throw new InvalidOperationException("Docházku lze znovu naimportovat jen ve stavu Rozpracovaný.");
            }

            return await ReimportAsync(
                existingTimesheet,
                employeeId,
                importedTimesheet,
                validInterruptionCodes,
                cancellationToken);
        }

        Data.Models.TimesheetStatus draftStatus = await dbContext.TimesheetStatuses
            .AsNoTracking()
            .SingleAsync(s => s.Name == "Rozpracovaný", cancellationToken);

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
        return timesheet.Id;
    }

    private async Task<Guid> ReimportAsync(
        Data.Models.AttendanceTimesheet existingTimesheet,
        Guid employeeId,
        AttendanceTimesheet importedTimesheet,
        HashSet<string> validInterruptionCodes,
        CancellationToken cancellationToken)
    {
        Guid timesheetId = existingTimesheet.Id;

        await dbContext.AttendanceDays
            .Where(day => day.AttendanceTimesheetId == timesheetId)
            .ExecuteDeleteAsync(cancellationToken);

        AddImportedDays(timesheetId, importedTimesheet, validInterruptionCodes);
        await UpsertEmployeeWorkloadAsync(employeeId, importedTimesheet.Year, importedTimesheet.Month, importedTimesheet.Workload, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.AttendanceTimesheets
            .Where(t => t.Id == timesheetId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        await RecalculateDraftProjectColumnsAsync(employeeId, importedTimesheet.Year, importedTimesheet.Month, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return timesheetId;
    }

    private void AddImportedDays(
        Guid attendanceTimesheetId,
        AttendanceTimesheet importedTimesheet,
        HashSet<string> validInterruptionCodes)
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

    private async Task RecalculateDraftProjectColumnsAsync(
        Guid employeeId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        Data.Models.AttendanceTimesheet? attendanceTimesheet = await dbContext.AttendanceTimesheets
            .AsNoTracking()
            .Include(t => t.Days)
            .FirstOrDefaultAsync(
                t => t.EmployeeId == employeeId && t.Year == year && t.Month == month,
                cancellationToken);

        if (attendanceTimesheet is null)
        {
            return;
        }

        Dictionary<DateTime, Data.Models.AttendanceDay> attendanceByDate = attendanceTimesheet.Days
            .ToDictionary(day => ToUtcDate(day.Date).Date);

        List<Data.Models.ProjectTimesheet> projectTimesheets = await dbContext.ProjectTimesheets
            .Include(pt => pt.Days)
            .Where(pt => pt.EmployeeId == employeeId && pt.Year == year && pt.Month == month)
            .Where(pt => pt.TimesheetStatusId == TimesheetWorkflowConstants.DraftStatusId)
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
                projectDay.HoursObligation = decimal.Round(attendanceDay.HoursObligation * projectTimesheet.Workload, 2, MidpointRounding.AwayFromZero);
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
