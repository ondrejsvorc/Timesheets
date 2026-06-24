using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Auth;
using Timesheets.Api.Common;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class AddProjectManager : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/{id}/managers", Handle)
           .WithSummary("Add Manager to Project")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request(Guid ProjectId, Guid EmployeeId);
    public sealed record Response(Guid ProjectId, Guid EmployeeId, string EmployeePersonalNumber, string EmployeeFullName);
    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Results<Created<Response>, NotFound, BadRequest<string>, ForbidHttpResult>> Handle(Guid id, [FromBody] Request request, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.Satisfies(UserRole.ProjectManager, projectId: id))
        {
            return TypedResults.Forbid();
        }

        if (request.ProjectId != id)
        {
            return TypedResults.BadRequest("ProjectId in body must match the project in the URL.");
        }

        bool projectExists = await dbContext.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == id, cancellationToken);
        if (!projectExists)
        {
            return TypedResults.NotFound();
        }

        var employee = await dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Id == request.EmployeeId)
            .Select(e => new { e.PersonalNumber, e.FullName, e.TitleBefore, e.TitleAfter })
            .FirstOrDefaultAsync(cancellationToken);
        if (employee is null)
        {
            return TypedResults.NotFound();
        }

        bool alreadyExists = await dbContext.ProjectManagers
            .AnyAsync(pm => pm.ProjectId == id && pm.EmployeeId == request.EmployeeId, cancellationToken);
        if (alreadyExists)
        {
            return TypedResults.BadRequest("Manager is already assigned to this project.");
        }

        var projectManager = new ProjectManager
        {
            Id = Guid.CreateVersion7(),
            ProjectId = id,
            EmployeeId = request.EmployeeId,
        };
        dbContext.ProjectManagers.Add(projectManager);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new Response(
            id,
            request.EmployeeId,
            employee.PersonalNumber,
            EmployeeNameFormatter.Format(employee.TitleBefore, employee.FullName, employee.TitleAfter));

        return TypedResults.Created($"/projects/{id}/managers/{request.EmployeeId}", response);
    }
}
