using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class ConfirmTimesheetImport : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/import", Handle)
           .WithSummary("Confirm and Import Timesheet Files")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record FileSelection(string FileName, Guid EmployeeId, Guid ContractId, int Year, int Month);
    public sealed record Request(IFormFileCollection Files, string SelectionsJson);
    public sealed record ImportResult(string FileName, bool Success, string ErrorMessage, Guid? TimesheetId);
    public sealed record Response(IEnumerable<ImportResult> Results);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Files)
                .NotNull().WithMessage("Soubory jsou povinné.")
                .Must(files => files.Count > 0).WithMessage("Alespoň jeden soubor je povinný.");

            RuleFor(x => x.SelectionsJson)
                .NotNull().WithMessage("Výběr souborů je povinný.")
                .NotEmpty().WithMessage("Výběr souborů je povinný.");
        }
    }

    private static async Task<Results<Ok<Response>, BadRequest<string>>> Handle([FromForm] Request request, [FromServices] ITimesheetImporter<AttendanceTimesheet> importer, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        List<FileSelection> selections;
        try
        {
            selections = JsonSerializer.Deserialize<List<FileSelection>>(request.SelectionsJson) ?? [];
        }
        catch
        {
            return TypedResults.BadRequest("Neplatný formát výběru souborů.");
        }

        if (selections.Count == 0)
        {
            return TypedResults.BadRequest("Alespoň jeden soubor musí být vybrán k importu.");
        }

        List<ImportResult> results = [];
        Dictionary<string, IFormFile> fileMap = request.Files.ToDictionary(file => file.FileName, f => f);

        Data.Models.TimesheetStatus draftStatus = await dbContext.TimesheetStatuses
            .AsNoTracking()
            .SingleAsync(s => s.Name == "Rozpracovaný", cancellationToken); // TODO: Replace with ID

        foreach (FileSelection selection in selections)
        {
            if (!fileMap.TryGetValue(selection.FileName, out IFormFile? file))
            {
                results.Add(new ImportResult(selection.FileName, Success: false, "Soubor nebyl nalezen.", TimesheetId: null));
                continue;
            }

            try
            {
                bool timesheetExists = await dbContext.AttendanceTimesheets
                    .AsNoTracking()
                    .AnyAsync(timesheet => timesheet.EmployeeId == selection.EmployeeId
                        && timesheet.ContractId == selection.ContractId
                        && timesheet.Year == selection.Year
                        && timesheet.Month == selection.Month, cancellationToken);

                if (timesheetExists)
                {
                    results.Add(new ImportResult(selection.FileName, false, "Výkaz pro toto období již existuje.", null));
                    continue;
                }

                await using Stream stream = file.OpenReadStream();
                AttendanceTimesheet importedTimesheet = await importer.ImportAsync(stream);

                Data.Models.AttendanceTimesheet timesheet = new()
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = selection.EmployeeId,
                    ContractId = selection.ContractId,
                    TimesheetStatusId = draftStatus.Id,
                    Year = selection.Year,
                    Month = selection.Month,
                    CreatedAt = DateTime.UtcNow
                };

                foreach (AttendanceDay day in importedTimesheet.Days)
                {
                    timesheet.Days.Add(new Data.Models.AttendanceDay
                    {
                        Id = Guid.NewGuid(),
                        AttendanceTimesheetId = timesheet.Id,
                        Date = day.Date,
                        ClockIn = day.ClockIn,
                        ClockOut = day.ClockOut,
                        BreakStart = day.BreakStart,
                        BreakEnd = day.BreakEnd,
                        Workload = day.Workload,
                        HoursWithoutBreak = day.TotalHours,
                        HoursObligation = day.TotalHoursObligation,
                        IsHoliday = day.IsHoliday,
                        Description = null,
                        Schedules = "[]"
                    });
                }

                dbContext.AttendanceTimesheets.Add(timesheet);
                await dbContext.SaveChangesAsync(cancellationToken);

                results.Add(new ImportResult(selection.FileName, Success: true, ErrorMessage: string.Empty, timesheet.Id));
            }
            catch (Exception ex)
            {
                results.Add(new ImportResult(selection.FileName, false, $"Chyba při importu: {ex.Message}", null));
            }
        }

        return TypedResults.Ok(new Response(results));
    }
}

