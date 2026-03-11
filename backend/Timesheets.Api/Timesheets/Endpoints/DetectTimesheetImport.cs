using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Timesheets;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class DetectTimesheetImport : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("/detect", Handle)
        .WithSummary("Detect Attendance Timesheet Metadata")
        .DisableAntiforgery()
        .WithRequestValidation<Request>();

    public sealed record Request(Guid EmployeeId, IFormFile File);
    public sealed record Response(AttendanceTimesheetDetectionResult Result);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.EmployeeId)
                .NotEmpty().WithMessage("Zaměstnanec je povinný.");

            RuleFor(x => x.File)
                .NotNull().WithMessage("Soubor je povinný.")
                .Must(file => Path.GetExtension(file.FileName).ToLowerInvariant() is ".xls" or ".xlsx")
                .WithMessage("Soubor musí být ve formátu .xls nebo .xlsx.");
        }
    }

    private static async Task<Results<Ok<Response>, BadRequest<string>>> Handle([FromForm] Request request, [FromServices] IAttendanceTimesheetImportService importService, CancellationToken cancellationToken)
    {
        AttendanceTimesheetDetectionResult result = await importService.DetectAsync(request.EmployeeId, request.File, cancellationToken);
        return TypedResults.Ok(new Response(result));
    }
}
