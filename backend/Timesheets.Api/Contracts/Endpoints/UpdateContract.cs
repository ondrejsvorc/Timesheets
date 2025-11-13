using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Timesheets.Api.Common.Extensions;

namespace Timesheets.Api.Contracts.Endpoints;

public sealed class UpdateContract : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPatch("/{id}", Handle)
           .WithSummary("Update Contract")
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