using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Timesheets.Api.Common.Extensions;

namespace Timesheets.Api.Employees.Endpoints;

public sealed class UpdateEmployee : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/{id}", Handle)
           .WithSummary("Update Employee")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request;
    public sealed record Response;
    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Results<Ok<Response>, NotFound, BadRequest<string>>> Handle(Guid id, [FromBody] Request request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
