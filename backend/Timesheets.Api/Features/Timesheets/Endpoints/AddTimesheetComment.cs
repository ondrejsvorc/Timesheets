using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Domain;
using Timesheets.Api.Domain.Models;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Timesheets.Endpoints;

public sealed class AddTimesheetComment : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/comments", Handle)
           .WithSummary("Add Timesheet Comment")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request(Guid EmployeeId, int Year, int Month, string Text);
    public sealed record CommentAuthor(Guid Id, string Name);
    public sealed record Response(Guid Id, string Type, DateTime CreatedAt, string Text, CommentAuthor Author);
    private sealed record EmployeeMonthScope(Guid TimesheetId, IReadOnlyDictionary<Guid, string> ContractPartLabels);

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

    private static async Task<Results<Created<Response>, NotFound, ForbidHttpResult>> Handle([FromBody] Request request, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!await user.CanAccessEmployeeAsync(request.EmployeeId, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        EmployeeMonthScope? timesheetScope = await LoadScopeAsync(request.EmployeeId, request.Year, request.Month, dbContext, cancellationToken);
        if (timesheetScope is null)
        {
            return TypedResults.NotFound();
        }

        Employee author = await dbContext.Employees
            .AsNoTracking()
            .FirstAsync(e => e.Id == user.EmployeeId, cancellationToken);

        Domain.Models.TimesheetComment comment = new()
        {
            Id = Guid.CreateVersion7(),
            TimesheetId = timesheetScope.TimesheetId,
            Text = request.Text.Trim(),
            AuthorEmployeeId = user.EmployeeId,
        };

        dbContext.TimesheetComments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        CommentAuthor commentAuthor = new(author.Id, author.DisplayName);
        Response response = new(comment.Id, Type: "message", comment.CreatedAt, comment.Text, commentAuthor);

        return TypedResults.Created($"/api/timesheets/comments/{comment.Id}", response);
    }

    private static async Task<EmployeeMonthScope?> LoadScopeAsync(Guid employeeId, int year, int month, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Guid? timesheetId = await dbContext.Timesheets
            .AsNoTracking()
            .Where(timesheet => timesheet.EmployeeId == employeeId && timesheet.Year == year && timesheet.Month == month)
            .Select(timesheet => (Guid?)timesheet.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (timesheetId is null)
        {
            return null;
        }

        List<(Guid Id, string ContractRegistrationNumber)> contractPartRows = await dbContext.ContractParts
            .AsNoTracking()
            .Where(part => part.TimesheetId == timesheetId.Value)
            .Join(dbContext.ContractEmployees.AsNoTracking(), timesheet => timesheet.ContractEmployeeId, contractEmployee => contractEmployee.Id, (timesheet, contractEmployee) => new { timesheet, contractEmployee })
            .Join(dbContext.Contracts.AsNoTracking(), x => x.contractEmployee.ContractId, contract => contract.Id, (x, contract) => new { x.timesheet.Id, contract.RegistrationNumber })
            .OrderBy(x => x.RegistrationNumber)
            .Select(x => new ValueTuple<Guid, string>(x.Id, x.RegistrationNumber))
            .ToListAsync(cancellationToken);

        Dictionary<Guid, string> labels = contractPartRows.ToDictionary(row => row.Id, row => row.ContractRegistrationNumber);

        return new EmployeeMonthScope(timesheetId.Value, labels);
    }
}
