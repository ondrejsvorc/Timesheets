using CzechHolidays;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;
using Timesheets.Api.Features.Auth;
using Timesheets.Api.Features.Timesheets;

namespace Timesheets.Api.Features.Timesheets.Endpoints;

public sealed class GetTimesheetOverview : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/overview", Handle)
           .WithSummary("Get Timesheet Overview");

    public sealed record Request([FromQuery] Guid EmployeeId, [FromQuery] int Year, [FromQuery] int Month);
    public sealed record OverviewItem(
        Guid? TimesheetId,
        string Kind,
        string Label,
        string? ContractRegistrationNumber,
        string? Position,
        decimal Workload,
        IEnumerable<string> Managers,
        string Status,
        Guid? ContractId,
        Guid? ProjectId);
    public sealed record Response(Guid EmployeeId, int Year, int Month, string Status, IEnumerable<OverviewItem> Items, TimesheetMonthSummary Summary);
    private sealed record ProjectRowSource(
        Guid TimesheetId,
        Guid ContractId,
        Guid ProjectId,
        string ContractRegistrationNumber,
        string Position,
        decimal Workload,
        string StatusCode);
    private sealed record ManagerRowSource(Guid ContractId, string FullName);

    private static async Task<Results<Ok<Response>, NotFound, ForbidHttpResult>> Handle([AsParameters] Request request, AppDbContext dbContext, ICzechHolidaysFactory holidaysFactory, ICurrentUser user, CancellationToken cancellationToken)
    {
        if (!await user.CanAccessEmployeeAsync(request.EmployeeId, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var attendanceInfo = await (
            from timesheet in dbContext.Timesheets.AsNoTracking()
            join attendance in dbContext.Attendances.AsNoTracking() on timesheet.Id equals attendance.TimesheetId
            where timesheet.EmployeeId == request.EmployeeId && timesheet.Year == request.Year && timesheet.Month == request.Month
            select new
            {
                timesheet.Id,
                Status = timesheet.TimesheetStatus.Name,
                Days = attendance.Days
                    .OrderBy(day => day.Date)
                    .Select(day => new TimesheetMonthSummaryDay(day.Date, day.IsHoliday, day.Description))
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (attendanceInfo is null)
        {
            return TypedResults.NotFound();
        }

        await ContractPartInitializer.EnsureForEmployeeMonthAsync(request.EmployeeId, request.Year, request.Month, dbContext, holidaysFactory, cancellationToken);

        List<ProjectRowSource> contractPartRows = await dbContext.ContractParts
            .AsNoTracking()
            .Where(part => part.TimesheetId == attendanceInfo.Id)
            .Join(
                dbContext.ContractEmployees.AsNoTracking(),
                part => part.ContractEmployeeId,
                contractEmployee => contractEmployee.Id,
                (part, contractEmployee) => new { part, contractEmployee })
            .Join(
                dbContext.Contracts.AsNoTracking(),
                x => x.contractEmployee.ContractId,
                contract => contract.Id,
                (x, contract) => new ProjectRowSource(
                    x.part.Id,
                    contract.Id,
                    contract.ProjectId,
                    contract.RegistrationNumber,
                    x.contractEmployee.Position,
                    x.part.Workload,
                    x.part.TimesheetStatus.Code))
            .ToListAsync(cancellationToken);

        HashSet<DateOnly> holidays = holidaysFactory.Create(request.Year).Select(holiday => holiday.Date).ToHashSet();
        List<TimesheetMonthSummaryDay> summaryDays = attendanceInfo.Days
            .Select(day => day with { IsHoliday = day.IsHoliday || holidays.Contains(DateOnly.FromDateTime(day.Date)) })
            .ToList();

        decimal totalProjectWorkload = contractPartRows.Sum(item => item.Workload);
        decimal totalWorkload = await TimesheetWorkloads.GetAsync(request.EmployeeId, request.Year, request.Month, dbContext, cancellationToken);
        decimal coreWorkload = Math.Max(0m, totalWorkload - totalProjectWorkload);
        TimesheetMonthSummary summary = TimesheetMonthSummaryCalculator.Compute(request.Year, request.Month, summaryDays, totalWorkload);

        List<OverviewItem> items =
        [
            new(attendanceInfo.Id, "core", "Kmen", null, null, coreWorkload, [], attendanceInfo.Status, null, null),
        ];

        Guid[] contractIds = contractPartRows.Select(row => row.ContractId).Distinct().ToArray();
        List<ManagerRowSource> managerRows = (await dbContext.ContractManagers
            .AsNoTracking()
            .Include(manager => manager.Employee)
            .Where(manager => contractIds.Contains(manager.ContractId))
            .OrderBy(manager => manager.Employee.Surname)
            .ThenBy(manager => manager.Employee.FirstName)
            .ToListAsync(cancellationToken))
            .Select(manager => new ManagerRowSource(manager.ContractId, manager.Employee.DisplayName))
            .ToList();
        ILookup<Guid, string> managersByContract = managerRows.ToLookup(manager => manager.ContractId, manager => manager.FullName);

        for (int index = 0; index < contractPartRows.Count; index++)
        {
            ProjectRowSource row = contractPartRows[index];
            string projectStatus = TimesheetWorkflow.ResolveContractPartDisplayStatus(row.StatusCode);

            items.Add(new OverviewItem(
                row.TimesheetId,
                "contractPart",
                $"Projektová činnost {index + 1}",
                row.ContractRegistrationNumber,
                row.Position,
                row.Workload,
                managersByContract[row.ContractId],
                projectStatus,
                row.ContractId,
                row.ProjectId
            ));
        }

        return TypedResults.Ok(new Response(request.EmployeeId, request.Year, request.Month, attendanceInfo.Status, items, summary));
    }
}
