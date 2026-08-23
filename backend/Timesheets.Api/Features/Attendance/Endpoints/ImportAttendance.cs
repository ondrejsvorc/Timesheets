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
using Timesheets.Api.Features.Employees;
using Timesheets.Api.Features.Timesheets;

namespace Timesheets.Api.Features.Attendance.Endpoints;

public sealed class ImportAttendance : IEndpoint
{
    private const string HalfDaySuffix = "p\u016flden";

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
        TimesheetEvaluator evaluator,
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
            Guid timesheetId = await PersistAsync(request.EmployeeId, attendance, dbContext, holidaysFactory, evaluator, cancellationToken);
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
        TimesheetEvaluator evaluator,
        CancellationToken cancellationToken)
    {
        HashSet<string> validInterruptionCodes = await dbContext.Interruptions
            .AsNoTracking()
            .Select(i => i.Name)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken);

        await EnsureForEmployeeMonthAsync(employeeId, importedTimesheet.Year, importedTimesheet.Month, dbContext, holidaysFactory, cancellationToken);

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
            if (existingTimesheet.TimesheetStatus.Code != TimesheetStatus.DraftCode)
            {
                throw new ImportException("Docházku lze znovu naimportovat jen ve stavu Rozpracovaný.");
            }

            return await ReimportAsync(existingTimesheet, employeeId, importedTimesheet, validInterruptionCodes, dbContext, evaluator, cancellationToken);
        }

        return await CreateTimesheetAsync(employeeId, importedTimesheet, validInterruptionCodes, dbContext, evaluator, cancellationToken);
    }

    private static async Task<Guid> CreateTimesheetAsync(
        Guid employeeId,
        AttendanceFile importedTimesheet,
        HashSet<string> validInterruptionCodes,
        AppDbContext dbContext,
        TimesheetEvaluator evaluator,
        CancellationToken cancellationToken)
    {
        Guid employeeTypeId = await dbContext.Employees
            .AsNoTracking()
            .Where(employee => employee.Id == employeeId)
            .Select(employee => employee.EmployeeTypeId)
            .SingleAsync(cancellationToken);

        Domain.Models.Timesheet timesheet = new()
        {
            Id = Guid.CreateVersion7(),
            EmployeeId = employeeId,
            TimesheetStatusId = TimesheetStatus.DraftId,
            Year = importedTimesheet.Year,
            Month = importedTimesheet.Month,
            CreatedAt = DateTime.UtcNow
        };

        TimesheetBootstrap.AddMonth(dbContext, timesheet, employeeTypeId);
        AddAttendanceDays(dbContext, timesheet.Id, importedTimesheet, validInterruptionCodes);

        await UpsertEmployeeWorkloadAsync(dbContext, employeeId, importedTimesheet.Year, importedTimesheet.Month, importedTimesheet.Workload, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await ApplyInterruptionHoursAsync(timesheet.Id, dbContext, evaluator, cancellationToken);
        return timesheet.Id;
    }

    private static async Task<Guid> ReimportAsync(
        Domain.Models.Timesheet existingTimesheet,
        Guid employeeId,
        AttendanceFile importedTimesheet,
        HashSet<string> validInterruptionCodes,
        AppDbContext dbContext,
        TimesheetEvaluator evaluator,
        CancellationToken cancellationToken)
    {
        Guid timesheetId = existingTimesheet.Id;
        Guid attendanceId = await dbContext.Attendances
            .Where(attendance => attendance.TimesheetId == timesheetId)
            .Select(attendance => attendance.Id)
            .SingleAsync(cancellationToken);

        List<Domain.Models.AttendanceDay> existingDays = await dbContext.AttendanceDays
            .Where(day => day.AttendanceId == attendanceId)
            .ToListAsync(cancellationToken);
        dbContext.AttendanceDays.RemoveRange(existingDays);

        AddAttendanceDays(dbContext, attendanceId, importedTimesheet, validInterruptionCodes);
        await UpsertEmployeeWorkloadAsync(dbContext, employeeId, importedTimesheet.Year, importedTimesheet.Month, importedTimesheet.Workload, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Timesheets
            .Where(t => t.Id == timesheetId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.UpdatedAt, DateTime.UtcNow), cancellationToken);

        await RecalculateDraftContractPartColumnsAsync(dbContext, employeeId, importedTimesheet.Year, importedTimesheet.Month, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await ApplyInterruptionHoursAsync(timesheetId, dbContext, evaluator, cancellationToken);
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
                HoursWithoutBreak = TimesheetEvaluator.CalculateWorkedHoursFromAttendance(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd),
                HoursObligation = TimesheetEvaluator.CalculateTotalHoursObligation(day.Date, day.IsHoliday, day.Workload),
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
            .Where(pt => pt.TimesheetStatus.Code == TimesheetStatus.DraftCode)
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
                contractPartDay.HoursObligation = TimesheetEvaluator.CalculateTotalHoursObligation(contractPartDay.Date, attendanceDay.IsHoliday, projectTimesheet.Workload);
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

    public static string? NormalizeInterruptions(string? raw, HashSet<string> validCodes)
    {
        if (string.IsNullOrWhiteSpace(raw) || validCodes.Count == 0)
        {
            return null;
        }

        List<string> normalized = [];
        foreach (string segment in raw.Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            bool halfDay = IsHalfDayInterruption(segment);
            string cleaned = Regex.Replace(segment, @"\([^)]*\)", " ");
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
                    string value = halfDay ? $"{resolved} {HalfDaySuffix}" : resolved;
                    if (!normalized.Contains(value, StringComparer.OrdinalIgnoreCase))
                    {
                        normalized.Add(value);
                    }
                }
            }
        }

        return normalized.Count == 0 ? null : string.Join(",", normalized);
    }

    private static bool IsHalfDayInterruption(string raw)
    {
        string upper = raw.ToUpperInvariant();
        return upper.Contains("P\u016eLDEN", StringComparison.Ordinal) || upper.Contains("PULDEN", StringComparison.Ordinal);
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

    private static async Task EnsureForEmployeeMonthAsync(Guid employeeId, int year, int month, AppDbContext dbContext, ICzechHolidaysFactory holidaysFactory, CancellationToken cancellationToken)
    {
        DateTime periodStart = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime periodEnd = periodStart.AddMonths(1).AddDays(-1);
        List<ContractEmployee> assignments = await dbContext.ContractEmployees
            .AsNoTracking()
            .Include(assignment => assignment.Contract)
            .ThenInclude(contract => contract.Project)
            .Where(assignment => assignment.EmployeeId == employeeId && assignment.StartDate <= periodEnd && (!assignment.EndDate.HasValue || assignment.EndDate >= periodStart))
            .Where(assignment => !assignment.Contract.Project.EndDate.HasValue || assignment.Contract.Project.EndDate >= periodStart)
            .ToListAsync(cancellationToken);

        if (assignments.Count == 0)
        {
            return;
        }

        Guid timesheetId = await TimesheetBootstrap.EnsureMonthTimesheetIdAsync(dbContext, employeeId, year, month, cancellationToken);
        Guid[] assignmentIds = assignments.Select(assignment => assignment.Id).ToArray();
        HashSet<Guid> existingAssignmentIds = await dbContext.ContractParts
            .AsNoTracking()
            .Where(part => part.TimesheetId == timesheetId && assignmentIds.Contains(part.ContractEmployeeId))
            .Select(part => part.ContractEmployeeId)
            .ToHashSetAsync(cancellationToken);
        List<ContractEmployee> missingAssignments = assignments.Where(assignment => !existingAssignmentIds.Contains(assignment.Id)).ToList();

        if (missingAssignments.Count == 0)
        {
            return;
        }

        HashSet<DateOnly> holidays = holidaysFactory.Create(year).Select(holiday => holiday.Date).ToHashSet();
        dbContext.ContractParts.AddRange(missingAssignments.Select(assignment => CreateContractPart(assignment, year, month, holidays, timesheetId)));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ContractPart CreateContractPart(ContractEmployee assignment, int year, int month, HashSet<DateOnly> holidays, Guid timesheetId)
    {
        ContractPart contractPart = new()
        {
            Id = Guid.CreateVersion7(),
            TimesheetId = timesheetId,
            ContractEmployeeId = assignment.Id,
            TimesheetStatusId = TimesheetStatus.DraftId,
            Workload = assignment.Workload,
            CreatedAt = DateTime.UtcNow,
        };

        ContractPartDateRange range = EffectiveContractPartRange(
            assignment.StartDate,
            assignment.EndDate,
            assignment.Contract?.Project?.StartDate ?? assignment.StartDate,
            assignment.Contract?.Project?.EndDate);
        for (int day = 1; day <= DateTime.DaysInMonth(year, month); day++)
        {
            DateTime date = new(year, month, day, 0, 0, 0, DateTimeKind.Utc);
            if (!range.Includes(date))
            {
                continue;
            }

            bool isHoliday = holidays.Contains(DateOnly.FromDateTime(date));
            contractPart.Days.Add(new ContractPartDay
            {
                Id = Guid.CreateVersion7(),
                ContractPartId = contractPart.Id,
                Date = date,
                Hours = 0m,
                IsHoliday = isHoliday,
                HoursObligation = TimesheetEvaluator.CalculateTotalHoursObligation(date, isHoliday, assignment.Workload),
            });
        }

        return contractPart;
    }

    private static async Task ApplyInterruptionHoursAsync(Guid timesheetId, AppDbContext dbContext, TimesheetEvaluator evaluator, CancellationToken cancellationToken)
    {
        LoadedTimesheet? loaded = await LoadAsync(timesheetId, dbContext, cancellationToken);
        if (loaded is null)
        {
            return;
        }

        EditableTimesheet sheet = evaluator.BuildEditableTimesheet(loaded, evaluator.CurrentEdit(loaded));
        bool tracksAttendance = EmployeeTypes.TracksAttendance(loaded.Attendance.EmployeeTypeId);
        foreach (EditableTimesheetDay day in sheet.Days)
        {
            TimesheetEvaluator.ApplyInterruptionToDayState(day, sheet.ContractParts, loaded.TotalWorkload, tracksAttendance);
        }

        TimesheetEdit request = new(
            Days: sheet.Days.Select(day => new DayEdit(day.Date, day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd, day.CoreHours, day.Description, day.Schedules)).ToList(),
            ContractParts: sheet.ContractParts.Select(project => new ContractPartEdit(
                project.Id,
                sheet.Days.Select(day => new ContractPartDayEdit(day.Date, day.ContractPartHours.GetValueOrDefault(project.Id), day.ContractPartHoursFixed.GetValueOrDefault(project.Id))).ToList())).ToList());
        ApplyEdits(loaded, request);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyEdits(LoadedTimesheet loaded, TimesheetEdit request)
    {
        Dictionary<DateOnly, Domain.Models.AttendanceDay> days = loaded.Attendance.Days.ToDictionary(day => DateOnly.FromDateTime(day.Date));
        foreach (DayEdit update in request.Days)
        {
            if (!days.TryGetValue(DateOnly.FromDateTime(update.Date), out Domain.Models.AttendanceDay? day))
            {
                continue;
            }

            day.ClockIn = update.ClockIn;
            day.ClockOut = update.ClockOut;
            day.BreakStart = update.BreakStart;
            day.BreakEnd = update.BreakEnd;
            day.CoreHours = TimesheetEvaluator.Normalize(update.CoreHours);
            day.Description = update.Description;
            day.Schedules = JsonSerializer.Serialize(update.Schedules ?? []);
            day.HoursWithoutBreak = TimesheetEvaluator.CalculateWorkedHoursFromAttendance(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd);
        }

        Dictionary<Guid, ContractPartEdit> projects = (request.ContractParts ?? []).ToDictionary(project => project.ContractEmployeeId);
        foreach (Domain.Models.ContractPart project in loaded.ContractParts)
        {
            if (loaded.ContractPartRanges.TryGetValue(project.ContractEmployeeId, out ContractPartDateRange? range))
            {
                foreach (Domain.Models.ContractPartDay day in project.Days.Where(day => !range.Includes(day.Date)))
                {
                    day.Hours = 0m;
                    day.HoursLocked = false;
                }
            }

            if (!projects.TryGetValue(project.ContractEmployeeId, out ContractPartEdit? update))
            {
                continue;
            }

            project.UpdatedAt = DateTime.UtcNow;
            if (project.LockedAt is not null)
            {
                continue;
            }

            Dictionary<DateOnly, Domain.Models.ContractPartDay> contractPartDays = project.Days.ToDictionary(day => DateOnly.FromDateTime(day.Date));

            foreach (ContractPartDayEdit contractPartDay in update.Days)
            {
                DateOnly date = DateOnly.FromDateTime(contractPartDay.Date);
                if (contractPartDays.TryGetValue(date, out Domain.Models.ContractPartDay? day))
                {
                    bool active = loaded.ContractPartRanges.TryGetValue(project.ContractEmployeeId, out range) && range.Includes(contractPartDay.Date);
                    bool halfDayInterruption = days.TryGetValue(date, out Domain.Models.AttendanceDay? attendanceDay) && TimesheetEvaluator.HasEditableHalfDayInterruption(attendanceDay.Description);
                    day.Hours = active ? TimesheetEvaluator.Normalize(contractPartDay.Hours) : 0m;
                    day.HoursLocked = active && !halfDayInterruption && contractPartDay.HoursLocked;
                }
            }
        }

        loaded.Timesheet.UpdatedAt = DateTime.UtcNow;
    }

    private static async Task<LoadedTimesheet?> LoadAsync(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Domain.Models.Timesheet? timesheet = await dbContext.Timesheets
            .Include(value => value.Employee)
            .Include(value => value.TimesheetStatus)
            .SingleOrDefaultAsync(value => value.Id == id, cancellationToken);

        if (timesheet is null)
        {
            return null;
        }

        Domain.Models.Attendance? attendance = await dbContext.Attendances
            .Include(value => value.Days)
            .SingleOrDefaultAsync(value => value.TimesheetId == id, cancellationToken);

        if (attendance is null)
        {
            return null;
        }

        List<Domain.Models.ContractPart> projects = await dbContext.ContractParts
            .Include(value => value.Days)
            .Where(value => value.TimesheetId == timesheet.Id)
            .ToListAsync(cancellationToken);

        Guid[] assignmentIds = projects.Select(project => project.ContractEmployeeId).ToArray();
        var rangeRows = await (
            from assignment in dbContext.ContractEmployees.AsNoTracking()
            join contract in dbContext.Contracts.AsNoTracking() on assignment.ContractId equals contract.Id
            join project in dbContext.Projects.AsNoTracking() on contract.ProjectId equals project.Id
            where assignmentIds.Contains(assignment.Id)
            select new
            {
                assignment.Id,
                assignment.StartDate,
                AssignmentEndDate = assignment.EndDate,
                ProjectStartDate = project.StartDate,
                ProjectEndDate = project.EndDate
            })
            .ToListAsync(cancellationToken);
        Dictionary<Guid, ContractPartDateRange> projectRanges = rangeRows.ToDictionary(
            row => row.Id,
            row => EffectiveContractPartRange(row.StartDate, row.AssignmentEndDate, row.ProjectStartDate, row.ProjectEndDate));

        decimal totalWorkload = await GetWorkloadAsync(timesheet.EmployeeId, timesheet.Year, timesheet.Month, dbContext, cancellationToken);
        decimal coreWorkload = Math.Max(0m, totalWorkload - projects.Sum(project => project.Workload));
        return new LoadedTimesheet(Timesheet: timesheet, Attendance: attendance, ContractParts: projects, ContractPartRanges: projectRanges, TotalWorkload: totalWorkload, CoreWorkload: coreWorkload);
    }

    private static async Task<decimal> GetWorkloadAsync(Guid employeeId, int year, int month, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        decimal? monthly = await dbContext.EmployeeWorkloads
            .AsNoTracking()
            .Where(workload => workload.EmployeeId == employeeId && workload.Year == year && workload.Month == month)
            .Select(workload => (decimal?)workload.Workload)
            .FirstOrDefaultAsync(cancellationToken);

        if (monthly.HasValue)
        {
            return monthly.Value;
        }

        DateTime periodStart = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime periodEnd = periodStart.AddMonths(1).AddDays(-1);

        return await dbContext.CoreEmployments
            .AsNoTracking()
            .Where(employment => employment.EmployeeId == employeeId)
            .Where(employment => employment.StartDate <= periodEnd && (employment.EndDate == null || employment.EndDate >= periodStart))
            .OrderByDescending(employment => employment.StartDate)
            .Select(employment => (decimal?)employment.Workload)
            .FirstOrDefaultAsync(cancellationToken) ?? 0m;
    }

    private static ContractPartDateRange EffectiveContractPartRange(DateTime assignmentStartDate, DateTime? assignmentEndDate, DateTime projectStartDate, DateTime? projectEndDate)
    {
        DateTime start = Max(ToUtcDate(assignmentStartDate), ToUtcDate(projectStartDate));
        DateTime? end = Min(assignmentEndDate.HasValue ? ToUtcDate(assignmentEndDate.Value) : null, projectEndDate.HasValue ? ToUtcDate(projectEndDate.Value) : null);
        return new ContractPartDateRange(start, end);
    }

    private static DateTime Max(DateTime first, DateTime second) => first >= second ? first : second;

    private static DateTime? Min(DateTime? first, DateTime? second) => (first, second) switch
    {
        (null, null) => null,
        (DateTime value, null) => value,
        (null, DateTime value) => value,
        (DateTime left, DateTime right) => left <= right ? left : right
    };
}
