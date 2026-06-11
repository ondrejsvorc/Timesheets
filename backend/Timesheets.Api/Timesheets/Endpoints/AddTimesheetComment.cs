using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Auth;
using Timesheets.Api.Common;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class AddTimesheetComment : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/combined/comments", Handle)
           .WithSummary("Add Combined Timesheet Comment")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request(Guid EmployeeId, int Year, int Month, string Text);
    public sealed record CommentAuthor(string Name, string Role);
    public sealed record Response(Guid Id, string Type, DateTime CreatedAt, string Text, CommentAuthor Author);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.EmployeeId).NotEmpty();
            RuleFor(x => x.Year).GreaterThan(0);
            RuleFor(x => x.Month).InclusiveBetween(1, 12);
            RuleFor(x => x.Text).NotEmpty().MaximumLength(500);
        }
    }

    private static async Task<Results<Created<Response>, NotFound, ForbidHttpResult>> Handle(
        [FromBody] Request request,
        AppDbContext dbContext,
        ICurrentUser user,
        CancellationToken cancellationToken)
    {
        if (!await user.CanAccessEmployeeAsync(request.EmployeeId, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        CombinedTimesheetScope? timesheetScope = await CombinedTimesheetScopeLoader.LoadAsync(
            request.EmployeeId,
            request.Year,
            request.Month,
            dbContext,
            cancellationToken);

        if (timesheetScope is null)
        {
            return TypedResults.NotFound();
        }

        Employee author = await dbContext.Employees
            .AsNoTracking()
            .FirstAsync(e => e.Id == user.EmployeeId, cancellationToken);

        Data.Models.TimesheetComment comment = new()
        {
            Id = Guid.NewGuid(),
            AttendanceTimesheetId = timesheetScope.AttendanceTimesheetId,
            Text = request.Text.Trim(),
            AuthorEmployeeId = user.EmployeeId,
        };

        dbContext.TimesheetComments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        Response response = new(
            comment.Id,
            "message",
            comment.CreatedAt,
            comment.Text,
            new CommentAuthor(
                EmployeeNameFormatter.Format(author.TitleBefore, author.FullName, author.TitleAfter),
                EmployeeRoleFormatter.FormatApiRole(author)));

        return TypedResults.Created($"/api/timesheets/combined/comments/{comment.Id}", response);
    }
}
