using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Auth;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class CreateProjectContract : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/{id}/contracts", Handle)
           .WithSummary("Create Contract in Project")
           .WithRequestValidation<Request>();

    public sealed record Request(string Name, string RegistrationNumber);
    public sealed record Response(ProjectContractItem ProjectContract);
    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Results<Created<Response>, NotFound, BadRequest<string>, ForbidHttpResult>> Handle(
        Guid id,
        [FromBody] Request request,
        AppDbContext dbContext,
        ICurrentUser user,
        CancellationToken cancellationToken)
    {
        if (!user.IsGlobalManagerRole())
        {
            return TypedResults.Forbid();
        }

        bool projectExists = await dbContext.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == id, cancellationToken);

        if (!projectExists)
        {
            return TypedResults.NotFound();
        }

        Contract contract = new()
        {
            Id = Guid.NewGuid(),
            ProjectId = id,
            Name = request.Name,
            RegistrationNumber = request.RegistrationNumber,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        dbContext.Contracts.Add(contract);
        await dbContext.SaveChangesAsync(cancellationToken);

        ProjectContractItem projectContract = new(
            contract.Id,
            request.Name,
            request.RegistrationNumber,
            EmployeeCount: 0
        );

        return TypedResults.Created($"/projects/{id}/contracts/{contract.Id}", new Response(projectContract));
    }
}
