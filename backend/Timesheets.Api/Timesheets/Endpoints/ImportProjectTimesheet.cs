using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Timesheets.Api.Common.Extensions;

namespace Timesheets.Api.Timesheets.Endpoints;

[Obsolete]
public sealed class ImportProjectTimesheet : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("/project/import", Handle)
        .WithSummary("Create Project Timesheet")
        .DisableAntiforgery()
        .WithRequestValidation<Request>();

    public sealed record Request(IFormFile File);
    public sealed record Response(ProjectTimesheet Timesheet);
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.File)
                .NotNull().WithMessage("Soubor je povinný.")
                .Must(file => Path.GetExtension(file.FileName).ToLowerInvariant() is ".xls" or ".xlsx")
                .WithMessage("Soubor musí být ve formátu .xls nebo .xlsx.");
        }
    }

    private static async Task<Results<Ok<Response>, BadRequest<string>>> Handle([FromForm] Request request, [FromServices] ITimesheetImporter<ProjectTimesheet> importer)
    {
        await using var stream = request.File.OpenReadStream();
        ProjectTimesheet timesheet = await importer.ImportAsync(stream);
        Response response = new(timesheet);
        return TypedResults.Ok(response);
    }
}