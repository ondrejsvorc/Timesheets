using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Domain;
using Timesheets.Api.Features.Auth;
using Timesheets.Api.Features.Contracts;

namespace Timesheets.Api.Features.Projects.Endpoints;

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

        Domain.Models.Project? currentProject = await dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(project => project.Id == id, cancellationToken);
        if (currentProject is null)
        {
            return TypedResults.NotFound();
        }

        if (currentProject.IsArchived())
        {
            return TypedResults.BadRequest(ProjectArchiveGuard.BlockMessage);
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

        ProjectItem? project = (await dbContext.Projects
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new
            {
                Project = p,
                ContractCount = p.Contracts.Count
            })
            .FirstOrDefaultAsync(cancellationToken)) is { } row
            ? new ProjectItem(row.Project.Id, row.Project.Name, row.Project.RegistrationNumber, row.Project.StartDate, row.Project.EndDate, row.Project.ArchivedAt, row.ContractCount, row.Project.GetStatus(PragueClock.Today))
            : null;

        return project is null ? TypedResults.NotFound() : TypedResults.Ok(new Response(project));
    }
}
