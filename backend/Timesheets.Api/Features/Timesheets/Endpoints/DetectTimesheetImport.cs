using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Features.Auth;
using Timesheets.Api.Features.Timesheets;

namespace Timesheets.Api.Features.Timesheets.Endpoints;

public sealed class DetectTimesheetImport : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("/detect", Handle)
        .WithSummary("Detect Attendance Timesheet Metadata")
        .DisableAntiforgery()
        .WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = AttendanceImport.MaxMultipartBodySizeBytes })
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
                .Custom((file, context) =>
                {
                    if (file is not null && AttendanceImport.GetFileValidationError(file) is string error)
                    {
                        context.AddFailure(error);
                    }
                });
        }
    }

    private static async Task<Results<Ok<Response>, BadRequest<string>, ForbidHttpResult>> Handle([FromForm] Request request, AttendanceImport import, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.IsGlobalManagerRole() && user.EmployeeId != request.EmployeeId)
        {
            return TypedResults.Forbid();
        }

        AttendanceTimesheetDetectionResult result = await import.DetectAsync(request.EmployeeId, request.File, cancellationToken);
        return TypedResults.Ok(new Response(result));
    }
}
