using CzechHolidays;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Auth;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;
using Timesheets.Api.Timesheets;

namespace Timesheets.Api.Contracts.Endpoints;

public sealed class UpdateContractEmployee : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/{id}/employees/{contractEmployeeId}", Handle)
           .WithSummary("Update Employee Position in Contract")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request(string PositionCode, string Position, decimal Workload, DateTime StartDate, DateTime? EndDate);
    public sealed record Response(
        Guid Id,
        Guid ContractId,
        Guid EmployeeId,
        string PositionCode,
        string Position,
        decimal Workload,
        DateTime StartDate,
        DateTime? EndDate);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.PositionCode).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Position).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Workload).GreaterThan(0);
        }
    }

    private static async Task<Results<Ok<Response>, NotFound, BadRequest<string>, ForbidHttpResult>> Handle(
        Guid id,
        Guid contractEmployeeId,
        [FromBody] Request request,
        AppDbContext dbContext,
        ICzechHolidaysFactory holidaysFactory,
        ICurrentUser user,
        CancellationToken cancellationToken)
    {
        if (!user.Satisfies(UserRole.ContractManager, contractId: id))
        {
            return TypedResults.Forbid();
        }

        ContractEmployee? existing = await dbContext.ContractEmployees
            .FirstOrDefaultAsync(ce => ce.ContractId == id && ce.Id == contractEmployeeId, cancellationToken);

        if (existing is null)
        {
            return TypedResults.NotFound();
        }

        ContractEmployeeUpdateRequest updateRequest = new(
            request.PositionCode,
            request.Position,
            request.Workload,
            request.StartDate,
            request.EndDate);

        ContractEmployeeUpdateImpact impact = await ContractEmployeeUpdatePlanner.PlanAsync(
            existing,
            updateRequest,
            dbContext,
            cancellationToken);

        if (!impact.CanUpdate)
        {
            return TypedResults.BadRequest(impact.BlockReason ?? "Pozici nelze upravit.");
        }

        ContractEmployee resultAssignment;

        if (impact.CreatesNewAssignment)
        {
            bool overlapping = await ContractEmployeeValidation.HasOverlappingSamePositionAsync(
                id,
                existing.EmployeeId,
                request.Position,
                impact.NewAssignmentStartDate!.Value,
                request.EndDate,
                contractEmployeeId,
                dbContext,
                cancellationToken);

            if (overlapping)
            {
                return TypedResults.BadRequest("Zam?stnanec u? m? tuto pozici na zak?zce v p?ekr?vaj?c?m se obdob?.");
            }

            string? workloadError = await ContractEmployeeValidation.ValidateMonthlyWorkloadAsync(
                existing.EmployeeId,
                request.Workload,
                impact.NewAssignmentStartDate!.Value,
                request.EndDate,
                contractEmployeeId,
                dbContext,
                cancellationToken);

            if (workloadError is not null)
            {
                return TypedResults.BadRequest(workloadError);
            }

            existing.EndDate = impact.CurrentAssignmentEndDate;

            ContractEmployee replacement = new()
            {
                Id = Guid.NewGuid(),
                ContractId = id,
                EmployeeId = existing.EmployeeId,
                PositionCode = request.PositionCode,
                Position = request.Position,
                Workload = request.Workload,
                StartDate = impact.NewAssignmentStartDate!.Value,
                EndDate = request.EndDate,
            };

            dbContext.ContractEmployees.Add(replacement);
            await EnsureTimesheetsForAssignmentAsync(replacement, dbContext, holidaysFactory, cancellationToken);
            resultAssignment = replacement;
        }
        else
        {
            existing.EndDate = impact.CurrentAssignmentEndDate;
            if (impact.DraftDaysToRemove > 0 && impact.CurrentAssignmentEndDate.HasValue)
            {
                await RemoveDraftProjectDaysOutsideRangeAsync(
                    contractEmployeeId,
                    impact.CurrentAssignmentEndDate.Value,
                    dbContext,
                    cancellationToken);
            }

            await EnsureTimesheetsForAssignmentAsync(existing, dbContext, holidaysFactory, cancellationToken);
            resultAssignment = existing;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new Response(
            resultAssignment.Id,
            resultAssignment.ContractId,
            resultAssignment.EmployeeId,
            resultAssignment.PositionCode,
            resultAssignment.Position,
            resultAssignment.Workload,
            resultAssignment.StartDate,
            resultAssignment.EndDate));
    }

    private static async Task RemoveDraftProjectDaysOutsideRangeAsync(
        Guid contractEmployeeId,
        DateTime newEnd,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        List<Guid> dayIds = await dbContext.ProjectTimesheets
            .Where(t => t.ContractEmployeeId == contractEmployeeId)
            .Where(t => t.TimesheetStatusId == TimesheetWorkflowConstants.DraftStatusId)
            .SelectMany(t => t.Days)
            .Where(day => day.Date > newEnd)
            .Select(day => day.Id)
            .ToListAsync(cancellationToken);

        if (dayIds.Count == 0)
        {
            return;
        }

        await dbContext.ProjectDays
            .Where(day => dayIds.Contains(day.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static async Task EnsureTimesheetsForAssignmentAsync(
        ContractEmployee contractEmployee,
        AppDbContext dbContext,
        ICzechHolidaysFactory holidaysFactory,
        CancellationToken cancellationToken)
    {
        DateTime start = ContractEmployeeValidation.ToUtcDate(contractEmployee.StartDate);
        DateTime end = contractEmployee.EndDate.HasValue
            ? ContractEmployeeValidation.ToUtcDate(contractEmployee.EndDate.Value)
            : start;

        DateTime cursor = new(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime last = new(end.Year, end.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        while (cursor <= last)
        {
            await ProjectTimesheetProvisioner.EnsureForAssignmentMonthAsync(
                contractEmployee,
                cursor.Year,
                cursor.Month,
                dbContext,
                holidaysFactory,
                cancellationToken);
            cursor = cursor.AddMonths(1);
        }
    }
}
