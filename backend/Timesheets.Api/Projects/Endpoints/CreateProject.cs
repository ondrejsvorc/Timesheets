using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class CreateProject : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/", Handle)
           .WithSummary("Create Project")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request(string Name, string RegistrationNumber, string RecipientName, DateTime StartDate, DateTime? EndDate, string Description);
    public sealed record Response(ProjectItem Project);
    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Results<Created<Response>, BadRequest<string>>> Handle([FromBody] Request request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Project project = new()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            RegistrationNumber = request.RegistrationNumber,
            RecipientName = request.RecipientName,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);

        ProjectItem projectItem = new(
            project.Id,
            project.Name,
            project.RegistrationNumber,
            project.StartDate,
            project.EndDate,
            ContractCount: 0
        );

        return TypedResults.Created($"/projects/{project.Id}", new Response(projectItem));
    }
}
