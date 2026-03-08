using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class DeleteProjectContract : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/{projectId}/contracts/{contractId}", Handle)
           .WithSummary("Delete Project Contract");

    private static async Task<Results<NoContent, NotFound>> Handle(
        Guid projectId,
        Guid contractId,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        int affected = await dbContext.Contracts
            .Where(c => c.ProjectId == projectId && c.Id == contractId)
            .ExecuteDeleteAsync(cancellationToken);

        if (affected == 0)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }
}
