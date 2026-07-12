using System.Text.Json;
using System.Text.RegularExpressions;
using CzechHolidays;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Domain;
using Timesheets.Api.Domain.Models;
using Timesheets.Api.Features.Auth;
using Timesheets.Api.Features.Timesheets;

namespace Timesheets.Api.Features.Attendance.Endpoints;

public sealed class ImportAttendance : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("/", Handle)
        .WithSummary("Import Attendance")
        .DisableAntiforgery()
        .WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = AttendanceFileDetector.MaxMultipartBodySizeBytes })
        .WithRequestValidation<Request>();

    public sealed record Request(Guid EmployeeId, IFormFile File);
    public sealed record Response(Guid TimesheetId, int Year, int Month);
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator(AttendanceFileDetector detector)
        {
            RuleFor(x => x.EmployeeId)
                .NotEmpty().WithMessage("Zaměstnanec je povinný.");

            RuleFor(x => x.File)
                .NotNull().WithMessage("Soubor je povinný.")
                .Custom((file, context) =>
                {
                    if (file is not null && detector.GetFileValidationError(file) is string error)
                    {
                        context.AddFailure(error);
                    }
                });
        }
    }

    private static async Task<Results<Ok<Response>, BadRequest<string>, ForbidHttpResult>> Handle(
        [FromForm] Request request,
        AttendanceFileReader reader,
        AttendanceFileDetector detector,
        AppDbContext dbContext,
        ICzechHolidaysFactory holidaysFactory,
        ICurrentUser user,
        ILogger<ImportAttendance> logger,
        CancellationToken cancellationToken)
    {
        if (!user.IsGlobalManagerRole() && user.EmployeeId != request.EmployeeId)
        {
            return TypedResults.Forbid();
        }

        AttendanceFileDetectionResult detection = await detector.DetectAsync(request.File, request.EmployeeId, dbContext, cancellationToken);
        if (!detection.CanImport)
        {
            return TypedResults.BadRequest(detection.ErrorMessage ?? "Soubor nelze importovat.");
        }

        AttendanceFile attendance;
        try
        {
            await using Stream stream = request.File.OpenReadStream();
            attendance = EnrichWithHolidays(reader.Read(stream), holidaysFactory);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read attendance file {FileName}.", request.File.FileName);
            return TypedResults.BadRequest("Soubor se nepodařilo přečíst.");
        }

        try
        {
            Guid timesheetId = await PersistAsync(request.EmployeeId, attendance, dbContext, holidaysFactory, cancellationToken);
            return TypedResults.Ok(new Response(timesheetId, attendance.Year, attendance.Month));
        }
        catch (ImportException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to import attendance file {FileName}.", request.File.FileName);
            return TypedResults.BadRequest("Import se nepodařilo dokončit.");
        }
    }

    private static AttendanceFile EnrichWithHolidays(AttendanceFile attendance, ICzechHolidaysFactory holidaysFactory)
    {
        HashSet<DateOnly> holidays = holidaysFactory.Create(attendance.Year).Select(holiday => holiday.Date).ToHashSet();
        return attendance with
        {
            Days = attendance.Days
                .Select(day => day with { IsHoliday = holidays.Contains(DateOnly.FromDateTime(day.Date)) })
                .ToArray()
        };
    }

    private static async Task<Guid> PersistAsync(
        Guid employeeId,
        AttendanceFile importedTimesheet,
        AppDbContext dbContext,
        ICzechHolidaysFactory holidaysFactory,
        CancellationToken cancellationToken)
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
            throw new ImportException($"Nelze importovat. Projektové úvazky pro {importedTimesheet.Month:00}/{importedTimesheet.Year} jsou {projectWorkload:0.##}, ale importovaný celkový úvazek je {importedTimesheet.Workload:0.##}. Nejdřív upravte přiřazení na zakázky.");
        }

        Domain.Models.Timesheet? existingTimesheet = await dbContext.Timesheets
            .Include(timesheet => timesheet.TimesheetStatus)
            .FirstOrDefaultAsync(timesheet => timesheet.EmployeeId == employeeId && timesheet.Year == importedTimesheet.Year && timesheet.Month == importedTimesheet.Month, cancellationToken);

        if (existingTimesheet is not null)
        {
            if (existingTimesheet.TimesheetStatus.Code != TimesheetStatusCodes.Draft)
            {
                throw new ImportException("Docházku lze znovu naimportovat jen ve stavu Rozpracovaný.");
            }

            return await ReimportAsync(existingTimesheet, employeeId, importedTimesheet, validInterruptionCodes, dbContext, cancellationToken);
        }

        return await CreateTimesheetAsync(employeeId, importedTimesheet, validInterruptionCodes, dbContext, cancellationToken);
    }

    private static async Task<Guid> CreateTimesheetAsync(
        Guid employeeId,
        AttendanceFile importedTimesheet,
        HashSet<string> validInterruptionCodes,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
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
        AddAttendanceDays(dbContext, timesheet.Id, importedTimesheet, validInterruptionCodes);

        await UpsertEmployeeWorkloadAsync(dbContext, employeeId, importedTimesheet.Year, importedTimesheet.Month, importedTimesheet.Workload, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await TimesheetEngine.ApplyInterruptionHoursAsync(timesheet.Id, dbContext, cancellationToken);
        return timesheet.Id;
    }

    private static async Task<Guid> ReimportAsync(
        Domain.Models.Timesheet existingTimesheet,
        Guid employeeId,
        AttendanceFile importedTimesheet,
        HashSet<string> validInterruptionCodes,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        Guid timesheetId = existingTimesheet.Id;

        List<Domain.Models.AttendanceDay> existingDays = await dbContext.AttendanceDays
            .Where(day => day.AttendanceId == timesheetId)
            .ToListAsync(cancellationToken);
        dbContext.AttendanceDays.RemoveRange(existingDays);

        AddAttendanceDays(dbContext, timesheetId, importedTimesheet, validInterruptionCodes);
        await UpsertEmployeeWorkloadAsync(dbContext, employeeId, importedTimesheet.Year, importedTimesheet.Month, importedTimesheet.Workload, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Timesheets
            .Where(t => t.Id == timesheetId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.UpdatedAt, DateTime.UtcNow), cancellationToken);

        await RecalculateDraftContractPartColumnsAsync(dbContext, employeeId, importedTimesheet.Year, importedTimesheet.Month, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await TimesheetEngine.ApplyInterruptionHoursAsync(timesheetId, dbContext, cancellationToken);
        return timesheetId;
    }

    private static void AddAttendanceDays(AppDbContext dbContext, Guid attendanceId, AttendanceFile importedTimesheet, HashSet<string> validInterruptionCodes)
    {
        foreach (AttendanceFileDay day in importedTimesheet.Days)
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
                HoursWithoutBreak = TimesheetLogic.CalculateWorkedHoursFromAttendance(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd),
                HoursObligation = TimesheetLogic.CalculateTotalHoursObligation(day.Date, day.IsHoliday, day.Workload),
                IsHoliday = day.IsHoliday,
                Description = NormalizeInterruptions(day.OtherInterruption, validInterruptionCodes),
                Schedules = JsonSerializer.Serialize(day.Schedules)
            });
        }
    }

    private static async Task RecalculateDraftContractPartColumnsAsync(AppDbContext dbContext, Guid employeeId, int year, int month, CancellationToken cancellationToken)
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

    private static async Task UpsertEmployeeWorkloadAsync(AppDbContext dbContext, Guid employeeId, int year, int month, decimal workload, CancellationToken cancellationToken)
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

    private static string? NormalizeInterruptions(string? raw, HashSet<string> validCodes)
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

    private sealed class ImportException(string message) : Exception(message);
}
