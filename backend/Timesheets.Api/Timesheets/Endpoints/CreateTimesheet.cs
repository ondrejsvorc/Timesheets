using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class CreateTimesheet : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/", Handle)
           .WithSummary("Create Timesheet")
           .WithRequestValidation<Request>();

    public sealed record Request(Guid EmployeeId, int Year, int Month);
    public sealed record Response(Guid Id);
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Month).InclusiveBetween(1, 12);
        }
    }

    private static Task<Results<Created<Response>, BadRequest<string>, NotFound>> Handle([FromBody] Request request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Manual creation of empty attendance timesheets is intentionally not supported.
        return Task.FromResult<Results<Created<Response>, BadRequest<string>, NotFound>>(
            TypedResults.BadRequest("Ruční vytvoření prázdného docházkového výkazu není podporováno. Použijte import z IMIS.")
        );
    }
}

