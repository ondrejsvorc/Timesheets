using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class UpdateProject : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/{id}", Handle)
           .WithSummary("Update Project")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request(string Name, string RegistrationNumber, string RecipientName, DateTime StartDate, DateTime? EndDate, string Description);
    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Results<NoContent, NotFound, BadRequest<string>>> Handle(Guid id, [FromBody] Request request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        int affected = await dbContext.Projects
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Name, request.Name)
                .SetProperty(p => p.RegistrationNumber, request.RegistrationNumber)
                .SetProperty(p => p.RecipientName, request.RecipientName)
                .SetProperty(p => p.StartDate, request.StartDate)
                .SetProperty(p => p.EndDate, request.EndDate)
                .SetProperty(p => p.Description, request.Description)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        if (affected == 0)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }
}
