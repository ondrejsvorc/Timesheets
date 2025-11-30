using Microsoft.AspNetCore.Http.HttpResults;
using Timesheets.Api.Data;

namespace Timesheets.Api.Employees.Endpoints;

public sealed class GetEmployees : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/", Handle)
           .WithSummary("Get Employees");

    public sealed record Response;

    private static async Task<Ok<Response>> Handle(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}