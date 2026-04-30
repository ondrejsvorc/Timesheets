using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Employees.Endpoints;

public sealed class CreateEmployee : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/", Handle)
           .WithSummary("Create Employee")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request(Guid EmployeeTypeId, string PersonalNumber, string FullName, string Email, bool IsGlobalManager);
    public sealed record Response(Guid Id);
    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Results<Created<Response>, BadRequest<string>>> Handle([FromBody] Request request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Employee employee = new()
        {
            Id = Guid.NewGuid(),
            EmployeeTypeId = request.EmployeeTypeId,
            PersonalNumber = request.PersonalNumber,
            FullName = request.FullName,
            Email = request.Email,
            IsGlobalManager = request.IsGlobalManager,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/employees/{employee.Id}", new Response(employee.Id));
    }
}
