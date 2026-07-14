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

public sealed class AddContractEmployee : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/{id}/employees", Handle)
           .WithSummary("Add Employee to Contract")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request(Guid EmployeeId, string PositionCode, string Position, decimal Workload, DateTime StartDate, DateTime? EndDate);
    public sealed record Response(Guid ContractId, Guid EmployeeId, string PositionCode, string Position, decimal Workload, DateTime StartDate, DateTime? EndDate, string PersonalNumber, string FullName, Guid EmployeeTypeId);
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.EmployeeId).NotEmpty();
            RuleFor(x => x.PositionCode).NotEmpty().MaximumLength(ContractEmployeeSchema.PositionCode.MaxLength);
            RuleFor(x => x.Position).NotEmpty().MaximumLength(ContractEmployeeSchema.Position.MaxLength);
            RuleFor(x => x.Workload).GreaterThan(0);
            RuleFor(x => x.StartDate).NotEmpty();
            RuleFor(x => x.StartDate).LessThan(x => x.EndDate!.Value).When(x => x.EndDate.HasValue);
        }
    }

    private static async Task<Results<Created<Response>, NotFound, BadRequest<string>, ForbidHttpResult>> Handle(Guid id, [FromBody] Request request, AppDbContext dbContext, ICzechHolidaysFactory holidaysFactory, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!user.Satisfies(UserRole.ContractManager, contractId: id))
        {
            return TypedResults.Forbid();
        }

        Contract? contract = await dbContext.Contracts
            .AsNoTracking()
            .Include(c => c.Project)
            .Where(c => c.Id == id)
            .SingleOrDefaultAsync(cancellationToken);

        if (contract is null)
        {
            return TypedResults.NotFound();
        }

        if (contract.Project.IsArchived())
        {
            return TypedResults.BadRequest(ProjectArchiveGuard.BlockMessage);
        }

        string? projectRangeError = ContractEmployeeValidation.ValidateProjectRange(contract.Project, request.StartDate, request.EndDate);
        if (projectRangeError is not null)
        {
            return TypedResults.BadRequest(projectRangeError);
        }

        bool employeeExists = await dbContext.Employees
            .AsNoTracking()
            .AnyAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (!employeeExists)
        {
            return TypedResults.NotFound();
        }

        DateTime? effectiveEndDate = request.EndDate ?? contract.Project.EndDate;

        bool overlappingSamePositionExists = await dbContext.ContractEmployees
            .AsNoTracking()
            .AnyAsync(ce =>
                ce.ContractId == id
                && ce.EmployeeId == request.EmployeeId
                && ce.Position == request.Position
                // overlap check (inclusive): [StartDate, EndDate] intersects
                && (ce.EndDate == null || request.StartDate <= ce.EndDate)
                && (effectiveEndDate == null || ce.StartDate <= effectiveEndDate),
                cancellationToken);

        if (overlappingSamePositionExists)
        {
            return TypedResults.BadRequest("Employee already has this position in contract for overlapping period.");
        }

        ContractEmployeeAddImpact addImpact = await ContractEmployeeAddPlanner.PlanAsync(
            id,
            contract.Project.EndDate,
            new ContractEmployeeAddRequest(request.EmployeeId, request.StartDate, effectiveEndDate),
            dbContext,
            cancellationToken);
        if (!addImpact.CanAdd)
        {
            return TypedResults.BadRequest(addImpact.BlockReason ?? "Pozici nelze přidat.");
        }

        // Block assignment if it would exceed monthly base workload (imported or from core employment).
        DateTime start = request.StartDate.Kind == DateTimeKind.Utc ? request.StartDate : DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc);
        DateTime end = effectiveEndDate.HasValue
            ? (effectiveEndDate.Value.Kind == DateTimeKind.Utc ? effectiveEndDate.Value : DateTime.SpecifyKind(effectiveEndDate.Value, DateTimeKind.Utc))
            : start;

        DateTime cursor = new(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime last = new(end.Year, end.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        while (cursor <= last)
        {
            int year = cursor.Year;
            int month = cursor.Month;

            decimal? baseWorkload = await GetBaseWorkloadAsync(request.EmployeeId, year, month, dbContext, cancellationToken);
            decimal baseForValidation = baseWorkload is > 0m ? baseWorkload.Value : 1m;

            decimal currentProjectWorkload = await dbContext.ContractParts
                .AsNoTracking()
                .Where(t => t.Timesheet.EmployeeId == request.EmployeeId && t.Timesheet.Year == year && t.Timesheet.Month == month)
                .SumAsync(t => (decimal?)t.Workload, cancellationToken) ?? 0m;

            if (currentProjectWorkload + request.Workload > baseForValidation)
            {
                return TypedResults.BadRequest($"Nelze přiřadit pozici. Překročil by se celkový úvazek pro {month:00}/{year}.");
            }

            cursor = cursor.AddMonths(1);
        }

        ContractEmployee newContractEmployee = new()
        {
            Id = Guid.CreateVersion7(),
            ContractId = id,
            EmployeeId = request.EmployeeId,
            PositionCode = request.PositionCode,
            Position = request.Position,
            Workload = request.Workload,
            StartDate = request.StartDate,
            EndDate = effectiveEndDate,
        };

        dbContext.ContractEmployees.Add(newContractEmployee);

        await EnsureTimesheetsForAssignmentAsync(newContractEmployee, dbContext, holidaysFactory, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        Employee employee = await dbContext.Employees
            .AsNoTracking()
            .FirstAsync(e => e.Id == request.EmployeeId, cancellationToken);

        Response response = new Response(
            id,
            request.EmployeeId,
            request.PositionCode,
            request.Position,
            request.Workload,
            request.StartDate,
            effectiveEndDate,
            employee.PersonalNumber,
            employee.DisplayName,
            employee.EmployeeTypeId);

        return TypedResults.Created($"/contracts/{id}/employees/{request.EmployeeId}", response);
    }

    private static async Task EnsureTimesheetsForAssignmentAsync(ContractEmployee contractEmployee, AppDbContext dbContext, ICzechHolidaysFactory holidaysFactory, CancellationToken cancellationToken)
    {
        DateTime start = contractEmployee.StartDate.Kind == DateTimeKind.Utc ? contractEmployee.StartDate : DateTime.SpecifyKind(contractEmployee.StartDate, DateTimeKind.Utc);
        DateTime end = contractEmployee.EndDate.HasValue
            ? (contractEmployee.EndDate.Value.Kind == DateTimeKind.Utc ? contractEmployee.EndDate.Value : DateTime.SpecifyKind(contractEmployee.EndDate.Value, DateTimeKind.Utc))
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

    private static async Task<decimal?> GetBaseWorkloadAsync(Guid employeeId, int year, int month, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        DateTime periodStart = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime periodEnd = periodStart.AddMonths(1).AddDays(-1);

        decimal? monthly = await dbContext.EmployeeWorkloads
            .AsNoTracking()
            .Where(w => w.EmployeeId == employeeId && w.Year == year && w.Month == month)
            .Select(w => (decimal?)w.Workload)
            .FirstOrDefaultAsync(cancellationToken);
        if (monthly.HasValue)
        {
            return monthly.Value;
        }

        decimal? workload = await dbContext.CoreEmployments
            .AsNoTracking()
            .Where(e => e.EmployeeId == employeeId)
            .Where(e => e.StartDate <= periodEnd && (e.EndDate == null || e.EndDate >= periodStart))
            .OrderByDescending(e => e.StartDate)
            .Select(e => (decimal?)e.Workload)
            .FirstOrDefaultAsync(cancellationToken);

        return workload;
    }

    private static bool IsAssignmentActiveForMonth(ContractEmployee assignment, int year, int month)
    {
        DateTime periodStart = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime periodEnd = periodStart.AddMonths(1).AddDays(-1);
        DateTime start = ToUtcDate(assignment.StartDate);

        if (start > periodEnd)
        {
            return false;
        }

        if (!assignment.EndDate.HasValue)
        {
            return true;
        }

        DateTime end = ToUtcDate(assignment.EndDate.Value);
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
        DateTime start = Max(ToUtcDate(assignmentStartDate), ToUtcDate(projectStartDate));
        DateTime? end = Min(assignmentEndDate.HasValue ? ToUtcDate(assignmentEndDate.Value) : null, projectEndDate.HasValue ? ToUtcDate(projectEndDate.Value) : null);
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

    private static DateTime ToUtcDate(DateTime value) => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
