using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Timesheets.Api.Common.Extensions;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class DeleteProject : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/{id}", Handle)
           .WithSummary("Delete Project")
           .WithRequestValidation<Request>();

    public sealed record Request;
    public sealed record Response;
    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Results<NoContent, NotFound>> Handle(Guid id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

