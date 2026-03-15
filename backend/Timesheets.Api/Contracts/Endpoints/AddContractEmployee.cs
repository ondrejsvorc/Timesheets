using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Contracts.Endpoints;

public sealed class AddContractEmployee : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/{id}/employees", Handle)
           .WithSummary("Add Employee to Contract")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request(Guid EmployeeId, string Position, decimal Workload, DateTime StartDate, DateTime? EndDate);
    public sealed record Response(Guid ContractId, Guid EmployeeId, string Position, decimal Workload, DateTime StartDate, DateTime? EndDate, int PersonalNumber, string FullName, Guid? EmployeeTypeId);
    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Results<Created<Response>, NotFound, BadRequest<string>>> Handle(Guid id, [FromBody] Request request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        bool contractExists = await dbContext.Contracts
            .AsNoTracking()
            .AnyAsync(c => c.Id == id, cancellationToken);

        if (!contractExists)
        {
            return TypedResults.NotFound();
        }

        bool employeeExists = await dbContext.Employees
            .AsNoTracking()
            .AnyAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (!employeeExists)
        {
            return TypedResults.NotFound();
        }

        bool alreadyExists = await dbContext.ContractEmployees
            .AsNoTracking()
            .AnyAsync(ce => ce.ContractId == id && ce.EmployeeId == request.EmployeeId && ce.Position == request.Position, cancellationToken);

        if (alreadyExists)
        {
            return TypedResults.BadRequest("Employee already has this position in contract.");
        }

        ContractEmployee newContractEmployee = new()
        {
            Id = Guid.NewGuid(),
            ContractId = id,
            EmployeeId = request.EmployeeId,
            Position = request.Position,
            Workload = request.Workload,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
        };

        dbContext.ContractEmployees.Add(newContractEmployee);
        await dbContext.SaveChangesAsync(cancellationToken);

        var employee = await dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Id == request.EmployeeId)
            .Select(e => new { e.PersonalNumber, e.FullName, e.EmployeeTypeId })
            .FirstAsync(cancellationToken);

        Response response = new Response(
            id,
            request.EmployeeId,
            request.Position,
            request.Workload,
            request.StartDate,
            request.EndDate,
            employee.PersonalNumber,
            employee.FullName,
            employee.EmployeeTypeId);

        return TypedResults.Created($"/contracts/{id}/employees/{request.EmployeeId}", response);
    }
}
