using Microsoft.AspNetCore.Http.HttpResults;
using Timesheets.Api.Data;

namespace Timesheets.Api.Employees.Endpoints;

public sealed class DeleteEmployee : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/{id}", Handle)
           .WithSummary("Delete Employee");

    private static async Task<Results<NoContent, NotFound>> Handle(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
