using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Auth;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Contracts;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class UpdateProject : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/{id}", Handle)
           .WithSummary("Update Project")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request(string Name, string RegistrationNumber, DateTime StartDate, DateTime? EndDate);
    public sealed record Response(ProjectItem Project);
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(ProjectSchema.Name.MaxLength);

            RuleFor(x => x.RegistrationNumber)
                .NotEmpty()
                .MaximumLength(ProjectSchema.RegistrationNumber.MaxLength);

            RuleFor(x => x.StartDate)
                .LessThan(x => x.EndDate)
                .When(x => x.EndDate.HasValue);
        }
    }

    private static async Task<Results<Ok<Response>, NotFound, BadRequest<string>, ForbidHttpResult>> Handle(Guid id, [FromBody] Request request, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.CanManageProject(id))
        {
            return TypedResults.Forbid();
        }

        bool projectExists = await dbContext.Projects.AsNoTracking().AnyAsync(project => project.Id == id, cancellationToken);
        if (!projectExists)
        {
            return TypedResults.NotFound();
        }

        DateTime startDate = ContractEmployeeValidation.ToUtcDate(request.StartDate);
        DateTime? endDate = request.EndDate.HasValue ? ContractEmployeeValidation.ToUtcDate(request.EndDate.Value) : null;
        bool assignmentOutsideRange = await dbContext.ContractEmployees
            .AsNoTracking()
            .Where(assignment => assignment.Contract.ProjectId == id)
            .AnyAsync(assignment => assignment.StartDate < startDate || endDate.HasValue && (!assignment.EndDate.HasValue || assignment.EndDate > endDate.Value), cancellationToken);

        if (assignmentOutsideRange)
        {
            return TypedResults.BadRequest("Projekt nelze zkrátit mimo období existujícího úvazku.");
        }

        try
        {
            await dbContext.Projects
                .Where(p => p.Id == id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Name, request.Name.Trim())
                    .SetProperty(p => p.RegistrationNumber, request.RegistrationNumber.Trim())
                    .SetProperty(p => p.StartDate, request.StartDate)
                    .SetProperty(p => p.EndDate, request.EndDate)
                    .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);
        }
        catch (DbUpdateException)
        {
            return TypedResults.BadRequest("Projekt s tímto Id nebo názvem už existuje.");
        }

        ProjectItem? project = await dbContext.Projects
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProjectItem(p.Id, p.Name, p.RegistrationNumber, p.StartDate, p.EndDate, p.ArchivedAt, p.Contracts.Count, p.Status))
            .FirstOrDefaultAsync(cancellationToken);

        return project is null ? TypedResults.NotFound() : TypedResults.Ok(new Response(project));
    }
}
