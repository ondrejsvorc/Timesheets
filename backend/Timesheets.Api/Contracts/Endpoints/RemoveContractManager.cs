using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;

namespace Timesheets.Api.Contracts.Endpoints;

public sealed class RemoveContractManager : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/{id}/managers/{employeeId}", Handle)
           .WithSummary("Remove Manager from Contract")
           .WithRequestValidation<Request>();

    public sealed record Request;
    public sealed record Response;
    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Results<NoContent, NotFound>> Handle(Guid id, Guid employeeId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
