using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Auth;
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
    public sealed record Response(ProjectContractItem ProjectContract);
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(ContractSchema.Name.MaxLength);
            RuleFor(x => x.RegistrationNumber).NotEmpty().MaximumLength(ContractSchema.RegistrationNumber.MaxLength);
        }
    }

    private static async Task<Results<Ok<Response>, NotFound, BadRequest<string>, ForbidHttpResult>> Handle(Guid projectId, Guid contractId, [FromBody] Request request, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.IsGlobalManagerRole())
        {
            return TypedResults.Forbid();
        }

        string name = request.Name.Trim();
        string registrationNumber = request.RegistrationNumber.Trim();
        bool exists = await dbContext.Contracts
            .AsNoTracking()
            .AnyAsync(c => c.ProjectId == projectId && c.Id != contractId && (c.Name == name || c.RegistrationNumber == registrationNumber), cancellationToken);
        if (exists)
        {
            return TypedResults.BadRequest("Zakázka s tímto Id nebo názvem už v projektu existuje.");
        }

        int affected = await dbContext.Contracts
            .Where(c => c.ProjectId == projectId && c.Id == contractId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.Name, name)
                .SetProperty(c => c.RegistrationNumber, registrationNumber)
                .SetProperty(c => c.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        if (affected == 0)
        {
            return TypedResults.NotFound();
        }

        ProjectContractItem? contract = await dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.ProjectId == projectId && c.Id == contractId)
            .Select(c => new ProjectContractItem(
                c.Id,
                c.Name,
                c.RegistrationNumber,
                c.ContractEmployees.Count))
            .FirstOrDefaultAsync(cancellationToken);

        return contract is null ? TypedResults.NotFound() : TypedResults.Ok(new Response(contract));
    }
}
