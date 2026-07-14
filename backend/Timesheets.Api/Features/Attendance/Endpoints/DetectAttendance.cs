using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Domain;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Attendance.Endpoints;

public sealed class DetectAttendance : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("/detect", Handle)
        .WithSummary("Detect Attendance File Metadata")
        .DisableAntiforgery()
        .WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = AttendanceFileDetector.MaxMultipartBodySizeBytes })
        .WithRequestValidation<Request>();

    public sealed record Request(Guid EmployeeId, IFormFile File);

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

    private static async Task<Results<Ok<AttendanceFileDetectionResult>, ForbidHttpResult>> Handle(
        [FromForm] Request request,
        AttendanceFileDetector detector,
        AppDbContext dbContext,
        ICurrentUser user,
        CancellationToken cancellationToken)
    {
        if (!user.IsGlobalManagerRole() && user.EmployeeId != request.EmployeeId)
        {
            return TypedResults.Forbid();
        }

        AttendanceFileDetectionResult result = await detector.DetectAsync(request.File, request.EmployeeId, dbContext, cancellationToken);
        return TypedResults.Ok(result);
    }
}
