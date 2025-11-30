using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;

namespace Timesheets.Api.Employees.Endpoints;

public sealed class CreateEmployee : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/", Handle)
           .WithSummary("Create Employee")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request;
    public sealed record Response(Guid Id);
    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Results<Created<Response>, BadRequest<string>>> Handle([FromBody] Request request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
