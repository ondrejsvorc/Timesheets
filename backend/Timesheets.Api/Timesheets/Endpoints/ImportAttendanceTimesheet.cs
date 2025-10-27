using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Timesheets.Api.Common.Extensions;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class ImportAttendanceTimesheet : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("/attendance/import", Handle)
        .WithSummary("Importuje výkaz pracovní doby.")
        .DisableAntiforgery()
        .WithRequestValidation<Request>();

    public sealed record Request(IFormFile File);
    public sealed record Response(AttendanceTimesheet Timesheet);
    public sealed class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(x => x.File)
                .NotNull().WithMessage("Soubor je povinný.")
                .Must(file => Path.GetExtension(file.FileName).ToLowerInvariant() is ".xls" or ".xlsx")
                .WithMessage("Soubor musí být ve formátu .xls nebo .xlsx.");
        }
    }

    private static async Task<Results<Ok<Response>, BadRequest<string>>> Handle([FromForm] Request request, [FromServices] ITimesheetImporter<AttendanceTimesheet> importer)
    {
        await using var stream = request.File.OpenReadStream();
        AttendanceTimesheet timesheet = await importer.ImportAsync(stream);
        Response response = new(timesheet);
        return TypedResults.Ok(response);
    }
}
