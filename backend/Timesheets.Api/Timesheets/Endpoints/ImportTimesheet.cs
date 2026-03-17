using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Timesheets;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class ImportTimesheet : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("/", Handle)
        .WithSummary("Create Attendance Timesheet")
        .DisableAntiforgery()
        .WithRequestValidation<Request>();

    public sealed record Request(Guid EmployeeId, IFormFile File);
    public sealed record Response(AttendanceTimesheetImportResult Result);
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

    private static async Task<Results<Ok<Response>, BadRequest<string>>> Handle(
        [FromForm] Request request,
        [FromServices] IAttendanceTimesheetImportService importService,
        CancellationToken cancellationToken)
    {
        AttendanceTimesheetImportResult result = await importService.ImportAsync(request.EmployeeId, request.File, cancellationToken);
        return TypedResults.Ok(new Response(result));
    }
}
