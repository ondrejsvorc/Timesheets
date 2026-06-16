using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Auth;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Contracts.Endpoints;

public sealed class AddContractManager : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/{id}/managers", Handle)
           .WithSummary("Add Manager to Contract")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request(Guid ContractId, Guid EmployeeId);
    public sealed record Response(Guid ContractId, Guid EmployeeId, string ContractRegistrationNumber, string EmployeePersonalNumber, string EmployeeFullName);
    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Results<Created<Response>, NotFound, BadRequest<string>, ForbidHttpResult>> Handle(Guid id, [FromBody] Request request, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.Satisfies(UserRole.ProjectManager, contractId: id))
        {
            return TypedResults.Forbid();
        }

        if (request.ContractId != id)
        {
            return TypedResults.BadRequest("ContractId in body must match the contract in the URL.");
        }

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

        bool alreadyExists = await dbContext.ContractManagers
            .AnyAsync(cm => cm.ContractId == id && cm.EmployeeId == request.EmployeeId, cancellationToken);
        if (alreadyExists)
        {
            return TypedResults.BadRequest("Manager is already assigned to this contract.");
        }

        var contractManager = new ContractManager
        {
            Id = Guid.NewGuid(),
            ContractId = id,
            EmployeeId = request.EmployeeId,
        };
        dbContext.ContractManagers.Add(contractManager);
        await dbContext.SaveChangesAsync(cancellationToken);

        var contract = await dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new { c.RegistrationNumber })
            .FirstAsync(cancellationToken);
        var employee = await dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Id == request.EmployeeId)
            .Select(e => new { e.PersonalNumber, e.FullName })
            .FirstAsync(cancellationToken);

        var response = new Response(
            id,
            request.EmployeeId,
            contract.RegistrationNumber,
            employee.PersonalNumber,
            employee.FullName);

        return TypedResults.Created($"/contracts/{id}/managers/{request.EmployeeId}", response);
    }
}
