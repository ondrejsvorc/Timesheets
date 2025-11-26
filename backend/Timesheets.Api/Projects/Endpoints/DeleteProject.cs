using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class DeleteProject : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/{id}", Handle)
           .WithSummary("Delete Project");

    private static async Task<Results<NoContent, NotFound>> Handle(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        int affected = await dbContext.Projects
            .Where(p => p.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        if (affected == 0)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }
}

