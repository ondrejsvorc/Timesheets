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
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(ContractSchema.Name.MaxLength);
            RuleFor(x => x.RegistrationNumber).NotEmpty().MaximumLength(ContractSchema.RegistrationNumber.MaxLength);
        }
    }

    private static async Task<Results<Created<Response>, NotFound, BadRequest<string>, ForbidHttpResult>> Handle(Guid id, [FromBody] Request request, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.CanManageProject(id))
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
            Name = request.Name.Trim(),
            RegistrationNumber = request.RegistrationNumber.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        dbContext.Contracts.Add(contract);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return TypedResults.BadRequest("Zakázka s tímto Id nebo názvem už v projektu existuje.");
        }

        ProjectContractItem projectContract = new(
            contract.Id,
            contract.Name,
            contract.RegistrationNumber,
            EmployeeCount: 0
        );

        return TypedResults.Created($"/projects/{id}/contracts/{contract.Id}", new Response(projectContract));
    }
}
