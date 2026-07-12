using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Domain;
using Timesheets.Api.Domain.Models;
using Timesheets.Api.Features.Auth;

namespace Timesheets.Api.Features.Contracts.Endpoints;

public sealed class GetContractEmployeeUpdateImpact : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/{id}/employees/{contractEmployeeId}/update-impact", Handle)
           .WithSummary("Get Contract Employee Update Impact")
           .DisableAntiforgery()
           .WithRequestValidation<ContractEmployeeUpdateRequest>();

    public sealed class Validator : AbstractValidator<ContractEmployeeUpdateRequest>
    {
        public Validator()
        {
            RuleFor(x => x.PositionCode).NotEmpty().MaximumLength(ContractEmployeeSchema.PositionCode.MaxLength);
            RuleFor(x => x.Position).NotEmpty().MaximumLength(ContractEmployeeSchema.Position.MaxLength);
            RuleFor(x => x.Workload).GreaterThan(0);
            RuleFor(x => x.StartDate).NotEmpty();
            RuleFor(x => x.StartDate)
                .LessThan(x => x.EndDate!.Value)
                .When(x => x.EndDate.HasValue);
        }
    }

    private static async Task<Results<Ok<ContractEmployeeUpdateImpact>, NotFound, ForbidHttpResult>> Handle(Guid id, Guid contractEmployeeId, [FromBody] ContractEmployeeUpdateRequest request, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.Satisfies(UserRole.ContractManager, contractId: id))
        {
            return TypedResults.Forbid();
        }

        ContractEmployee? existing = await dbContext.ContractEmployees
            .AsNoTracking()
            .FirstOrDefaultAsync(ce => ce.ContractId == id && ce.Id == contractEmployeeId, cancellationToken);

        if (existing is null)
        {
            return TypedResults.NotFound();
        }

        ContractEmployeeUpdateImpact impact = await ContractEmployeeUpdatePlanner.PlanAsync(
            existing,
            request,
            dbContext,
            cancellationToken);

        return TypedResults.Ok(impact);
    }
}
