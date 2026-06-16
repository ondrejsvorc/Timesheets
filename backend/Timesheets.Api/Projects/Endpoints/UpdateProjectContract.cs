using System.Text.RegularExpressions;
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
        if (!user.CanManageContract(contractId, projectId))
        {
            return TypedResults.Forbid();
        }

        bool contractExists = await dbContext.Contracts.AsNoTracking().AnyAsync(contract => contract.ProjectId == projectId && contract.Id == contractId, cancellationToken);
        if (!contractExists)
        {
            return TypedResults.NotFound();
        }

        string name = request.Name.Trim();
        string registrationNumber = Regex.Replace(request.RegistrationNumber, @"\s+", "").Trim();
        if (await ProjectContractValidation.HasDuplicateAsync(projectId, contractId, name, registrationNumber, dbContext, cancellationToken))
        {
            return TypedResults.BadRequest(ProjectContractValidation.DuplicateError);
        }

        try
        {
            await dbContext.Contracts
                .Where(c => c.ProjectId == projectId && c.Id == contractId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.Name, name)
                    .SetProperty(c => c.RegistrationNumber, registrationNumber)
                    .SetProperty(c => c.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);
        }
        catch (DbUpdateException)
        {
            return TypedResults.BadRequest(ProjectContractValidation.DuplicateError);
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
