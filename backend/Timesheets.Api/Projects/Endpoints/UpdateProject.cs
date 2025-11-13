using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Timesheets.Api.Common.Extensions;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class UpdateProject : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/{id}", Handle)
           .WithSummary("Update Project")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request(Guid Id, string Name, string Identifier, DateOnly Start, DateOnly End, string Description);
    public sealed record Response(Guid Id, string Name, string Identifier, DateOnly Start, DateOnly End, string Description);
    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Results<Ok<Response>, NotFound, BadRequest<string>>> Handle(Guid id, [FromBody] Request request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
