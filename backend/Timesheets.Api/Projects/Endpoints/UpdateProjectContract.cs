using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class UpdateProjectContract : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/{projectId}/contracts/{contractId}", Handle)
           .WithSummary("Update Project Contract")
           .WithRequestValidation<Request>();

    public sealed record Request(string Name, string RegistrationNumber);
    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Results<NoContent, NotFound, BadRequest<string>>> Handle(
        Guid projectId,
        Guid contractId,
        [FromBody] Request request,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        int affected = await dbContext.Contracts
            .Where(c => c.ProjectId == projectId && c.Id == contractId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.Name, request.Name)
                .SetProperty(c => c.RegistrationNumber, request.RegistrationNumber)
                .SetProperty(c => c.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        if (affected == 0)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }
}
