using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;

namespace Timesheets.Api.Employees.Endpoints;

public sealed class UpdateEmployee : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/{id}", Handle)
           .WithSummary("Update Employee")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request;
    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Results<NoContent, NotFound, BadRequest<string>>> Handle(Guid id, [FromBody] Request request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
