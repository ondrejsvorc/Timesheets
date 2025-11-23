using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Timesheets.Api.Common.Extensions;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class CreateProject : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/", Handle)
           .WithSummary("Create Project")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request(string Name, string Identifier, DateTime Start, DateTime End, string Description);
    public sealed record Response(Guid Id, string Name, string Identifier, DateTime Start, DateTime End, string Description);
    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Results<Created<Response>, BadRequest<string>>> Handle([FromBody] Request request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
