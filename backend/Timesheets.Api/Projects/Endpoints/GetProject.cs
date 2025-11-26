using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class GetProject : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}", Handle)
           .WithSummary("Get Project")
           .WithRequestValidation<Request>();

    public sealed record Request;
    public sealed record ContractItem(Guid Id, string Name, string Identifier, DateOnly Start, DateOnly End, int EmployeeCount);
    public sealed record Response(Guid Id, string Name, string Identifier, IEnumerable<ContractItem> Contracts);
    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Results<Ok<Response>, NotFound>> Handle(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
