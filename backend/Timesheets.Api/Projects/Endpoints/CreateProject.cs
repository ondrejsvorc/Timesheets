using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
using Timesheets.Api.Auth;
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
                .MaximumLength(ProjectSchema.RegistrationNumber.MaxLength);

            RuleFor(x => x.StartDate)
                .LessThan(x => x.EndDate)
                .When(x => x.EndDate.HasValue);
        }
    }

    private static async Task<Results<Created<Response>, BadRequest<string>, ForbidHttpResult>> Handle(
        [FromBody] Request request,
        HttpContext httpContext,
        AppDbContext dbContext,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        (_, UserPermissionsScope scope) = await PermissionsScopeResolver.ResolveRequiredAsync(
            httpContext, dbContext, administrationOptions, cancellationToken);

        if (!ApiPermissions.CanModifyProjects(scope))
        {
            return TypedResults.Forbid();
        }

        Project project = new()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            RegistrationNumber = request.RegistrationNumber,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
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
            project.ArchivedAt,
            ContractCount: 0
        );

        return TypedResults.Created($"/projects/{project.Id}", new Response(projectItem));
    }
}
