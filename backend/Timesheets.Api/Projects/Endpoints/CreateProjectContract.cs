using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class CreateProjectContract : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/{projectId}/contracts", Handle)
           .WithSummary("Create Contract in Project")
           .WithRequestValidation<Request>();

    public sealed record Request(string Name, string? RegistrationNumber, DateTime StartDate, DateTime? EndDate, string? Description);
    public sealed record Response(Guid Id);
    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Results<Created<Response>, NotFound, BadRequest<string>>> Handle(Guid projectId, [FromBody] Request request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        bool projectExists = await dbContext.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == projectId, cancellationToken);

        if (!projectExists)
        {
            return TypedResults.NotFound();
        }

        Contract contract = new()
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        dbContext.Contracts.Add(contract);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/projects/{projectId}/contracts/{contract.Id}", new Response(contract.Id));
    }
}
