using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Timesheets.Api.Common.Extensions;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class CreateProjectContract : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/{projectId}/contracts", Handle)
           .WithSummary("Create Contract in Project")
           .WithRequestValidation<Request>();

    public sealed record Request(string Name, string Identifier, DateOnly Start, DateOnly End, string Description);
    public sealed record Response(Guid Id, string Name, string Identifier, DateOnly Start, DateOnly End, string Description);
    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Results<Created<Response>, NotFound, BadRequest<string>>> Handle(Guid projectId, [FromBody] Request request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
