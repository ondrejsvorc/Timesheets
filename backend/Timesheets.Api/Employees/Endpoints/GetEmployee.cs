using Microsoft.AspNetCore.Http.HttpResults;
using Timesheets.Api.Data;

namespace Timesheets.Api.Employees.Endpoints;

public sealed class GetEmployee : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}", Handle)
           .WithSummary("Get Employee");

    public sealed record Response;

    private static async Task<Results<Ok<Response>, NotFound>> Handle(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}