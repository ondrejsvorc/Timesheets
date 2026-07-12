using CzechHolidays;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Domain;
using Timesheets.Api.Domain.Models;
using Timesheets.Api.Features.Auth;
using Timesheets.Api.Features.Projects;
using Timesheets.Api.Features.Timesheets;

namespace Timesheets.Api.Features.Contracts.Endpoints;

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
            RuleFor(x => x.PositionCode).NotEmpty().MaximumLength(ContractEmployeeSchema.PositionCode.MaxLength);
            RuleFor(x => x.Position).NotEmpty().MaximumLength(ContractEmployeeSchema.Position.MaxLength);
            RuleFor(x => x.Workload).GreaterThan(0);
            RuleFor(x => x.StartDate).NotEmpty();
            RuleFor(x => x.StartDate)
                .LessThan(x => x.EndDate!.Value)
                .When(x => x.EndDate.HasValue);
        }
    }

    private static async Task<Results<Ok<Response>, NotFound, BadRequest<string>, ForbidHttpResult>> Handle(Guid id, Guid contractEmployeeId, [FromBody] Request request, AppDbContext dbContext, ICzechHolidaysFactory holidaysFactory, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.Satisfies(UserRole.ContractManager, contractId: id))
        {
            return TypedResults.Forbid();
        }

        if (await ProjectArchiveGuard.BlockIfContractArchivedAsync(id, dbContext, cancellationToken) is { } archiveBlock)
        {
            return TypedResults.BadRequest(archiveBlock);
        }

        ContractEmployee? existing = await dbContext.ContractEmployees
            .FirstOrDefaultAsync(ce => ce.ContractId == id && ce.Id == contractEmployeeId, cancellationToken);

        if (existing is null)
        {
            return TypedResults.NotFound();
        }

        DateTime? projectEndDate = await dbContext.Contracts
            .AsNoTracking()
            .Where(contract => contract.Id == id)
            .Select(contract => contract.Project.EndDate)
            .SingleAsync(cancellationToken);

        ContractEmployeeUpdateRequest updateRequest = new(
            request.PositionCode,
            request.Position,
            request.Workload,
            request.StartDate,
            request.EndDate ?? projectEndDate);

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
                updateRequest.EndDate,
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
                updateRequest.EndDate,
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
                Id = Guid.CreateVersion7(),
                ContractId = id,
                EmployeeId = existing.EmployeeId,
                PositionCode = request.PositionCode,
                Position = request.Position,
                Workload = request.Workload,
                StartDate = impact.NewAssignmentStartDate!.Value,
                EndDate = updateRequest.EndDate,
            };

            dbContext.ContractEmployees.Add(replacement);
            await EnsureTimesheetsForAssignmentAsync(replacement, dbContext, holidaysFactory, cancellationToken);
            resultAssignment = replacement;
        }
        else
        {
            if (request.Position != existing.Position)
            {
                bool overlapping = await ContractEmployeeValidation.HasOverlappingSamePositionAsync(
                    id,
                    existing.EmployeeId,
                    request.Position,
                    ContractEmployeeValidation.ToUtcDate(existing.StartDate),
                    updateRequest.EndDate,
                    contractEmployeeId,
                    dbContext,
                    cancellationToken);

                if (overlapping)
                {
                    return TypedResults.BadRequest("Zaměstnanec už má tuto pozici na zakázce v překrývajícím se období.");
                }
            }

            if (request.Workload != existing.Workload)
            {
                string? workloadError = await ContractEmployeeValidation.ValidateMonthlyWorkloadAsync(
                    existing.EmployeeId,
                    request.Workload,
                    ContractEmployeeValidation.ToUtcDate(existing.StartDate),
                    updateRequest.EndDate,
                    contractEmployeeId,
                    dbContext,
                    cancellationToken);

                if (workloadError is not null)
                {
                    return TypedResults.BadRequest(workloadError);
                }
            }

            existing.PositionCode = request.PositionCode;
            existing.Position = request.Position;
            existing.Workload = request.Workload;
            if (impact.CurrentAssignmentEndDate.HasValue)
            {
                existing.EndDate = impact.CurrentAssignmentEndDate;
            }
            if (impact.DraftDaysToRemove > 0 && impact.CurrentAssignmentEndDate.HasValue)
            {
                await RemoveDraftContractPartDaysOutsideRangeAsync(
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

    private static async Task RemoveDraftContractPartDaysOutsideRangeAsync(Guid contractEmployeeId, DateTime newEnd, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        List<Guid> dayIds = await dbContext.ContractParts
            .Where(t => t.ContractEmployeeId == contractEmployeeId)
            .Where(t => t.TimesheetStatus.Code == TimesheetStatus.DraftCode)
            .SelectMany(t => t.Days)
            .Where(day => day.Date > newEnd)
            .Select(day => day.Id)
            .ToListAsync(cancellationToken);

        if (dayIds.Count == 0)
        {
            return;
        }

        await dbContext.ContractPartDays
            .Where(day => dayIds.Contains(day.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static async Task EnsureTimesheetsForAssignmentAsync(ContractEmployee contractEmployee, AppDbContext dbContext, ICzechHolidaysFactory holidaysFactory, CancellationToken cancellationToken)
    {
        DateTime start = ContractEmployeeValidation.ToUtcDate(contractEmployee.StartDate);
        DateTime end = contractEmployee.EndDate.HasValue
            ? ContractEmployeeValidation.ToUtcDate(contractEmployee.EndDate.Value)
            : start;

        DateTime cursor = new(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime last = new(end.Year, end.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        while (cursor <= last)
        {
            Guid timesheetId = await TimesheetBootstrap.EnsureMonthTimesheetIdAsync(dbContext, contractEmployee.EmployeeId, cursor.Year, cursor.Month, cancellationToken);
            Domain.Models.ContractPart? existing = await dbContext.ContractParts
                .FirstOrDefaultAsync(
                    t => t.ContractEmployeeId == contractEmployee.Id && t.TimesheetId == timesheetId,
                    cancellationToken);

            if (existing is null)
            {
                await EnsureForAssignmentMonthAsync(contractEmployee, cursor.Year, cursor.Month, dbContext, holidaysFactory, cancellationToken);
            }
            else
            {
                existing.Workload = contractEmployee.Workload;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            cursor = cursor.AddMonths(1);
        }
    }

    private static bool IsAssignmentActiveForMonth(ContractEmployee assignment, int year, int month)
    {
        DateTime periodStart = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime periodEnd = periodStart.AddMonths(1).AddDays(-1);
        DateTime start = ContractEmployeeValidation.ToUtcDate(assignment.StartDate);

        if (start > periodEnd)
        {
            return false;
        }

        if (!assignment.EndDate.HasValue)
        {
            return true;
        }

        DateTime end = ContractEmployeeValidation.ToUtcDate(assignment.EndDate.Value);
        return end >= periodStart;
    }

    private static async Task<bool> EnsureForAssignmentMonthAsync(ContractEmployee assignment, int year, int month, AppDbContext dbContext, ICzechHolidaysFactory holidaysFactory, CancellationToken cancellationToken)
    {
        if (!IsAssignmentActiveForMonth(assignment, year, month))
        {
            return false;
        }

        Guid timesheetId = await TimesheetBootstrap.EnsureMonthTimesheetIdAsync(dbContext, assignment.EmployeeId, year, month, cancellationToken);
        bool exists = dbContext.ContractParts.Local.Any(part => part.TimesheetId == timesheetId && part.ContractEmployeeId == assignment.Id)
            || await dbContext.ContractParts.AnyAsync(part => part.TimesheetId == timesheetId && part.ContractEmployeeId == assignment.Id, cancellationToken);

        if (exists)
        {
            return false;
        }

        dbContext.ContractParts.Add(CreateContractPart(assignment, year, month, holidaysFactory.Create(year).Select(holiday => holiday.Date).ToHashSet(), timesheetId));
        return true;
    }

    private static ContractPart CreateContractPart(ContractEmployee assignment, int year, int month, HashSet<DateOnly> holidays, Guid timesheetId)
    {
        ContractPart contractPart = new()
        {
            Id = Guid.CreateVersion7(),
            TimesheetId = timesheetId,
            ContractEmployeeId = assignment.Id,
            TimesheetStatusId = TimesheetStatus.DraftId,
            Workload = assignment.Workload,
            CreatedAt = DateTime.UtcNow,
        };

        ContractPartDateRange range = EffectiveContractPartRange(
            assignment.StartDate,
            assignment.EndDate,
            assignment.Contract?.Project?.StartDate ?? assignment.StartDate,
            assignment.Contract?.Project?.EndDate);
        for (int day = 1; day <= DateTime.DaysInMonth(year, month); day++)
        {
            DateTime date = new(year, month, day, 0, 0, 0, DateTimeKind.Utc);
            if (!range.Includes(date))
            {
                continue;
            }

            bool isHoliday = holidays.Contains(DateOnly.FromDateTime(date));
            contractPart.Days.Add(new ContractPartDay
            {
                Id = Guid.CreateVersion7(),
                ContractPartId = contractPart.Id,
                Date = date,
                Hours = 0m,
                IsHoliday = isHoliday,
                HoursObligation = TimesheetEvaluator.CalculateTotalHoursObligation(date, isHoliday, assignment.Workload),
            });
        }

        return contractPart;
    }

    private static ContractPartDateRange EffectiveContractPartRange(DateTime assignmentStartDate, DateTime? assignmentEndDate, DateTime projectStartDate, DateTime? projectEndDate)
    {
        DateTime start = Max(ContractEmployeeValidation.ToUtcDate(assignmentStartDate), ContractEmployeeValidation.ToUtcDate(projectStartDate));
        DateTime? end = Min(assignmentEndDate.HasValue ? ContractEmployeeValidation.ToUtcDate(assignmentEndDate.Value) : null, projectEndDate.HasValue ? ContractEmployeeValidation.ToUtcDate(projectEndDate.Value) : null);
        return new ContractPartDateRange(start, end);
    }

    private static DateTime Max(DateTime first, DateTime second) => first >= second ? first : second;

    private static DateTime? Min(DateTime? first, DateTime? second) => (first, second) switch
    {
        (null, null) => null,
        (DateTime value, null) => value,
        (null, DateTime value) => value,
        (DateTime left, DateTime right) => left <= right ? left : right
    };
}
