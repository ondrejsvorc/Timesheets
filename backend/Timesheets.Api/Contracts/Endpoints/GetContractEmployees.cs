using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Timesheets.Api.Common.Extensions;

namespace Timesheets.Api.Contracts.Endpoints;

public sealed class GetContractEmployees : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}/employees", Handle)
           .WithSummary("Get Contract Employees")
           .WithRequestValidation<Request>();

    public sealed record Request;
    public sealed record Response;
    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Results<Ok<Response>, NotFound>> Handle(Guid id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

