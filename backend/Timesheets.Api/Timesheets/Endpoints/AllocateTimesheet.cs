using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Timesheets.Api.Auth;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Employees;
using Timesheets.Api.Timesheets;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class AllocateTimesheet : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/{id}/allocate", Handle)
            .WithSummary("Allocate Timesheet Draft")
            .WithRequestValidation<TimesheetDraft>();

    private static async Task<Results<Ok<TimesheetAllocation>, NotFound, ForbidHttpResult>> Handle(Guid id, [FromQuery] int? day, [FromBody] TimesheetDraft draft, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        TimesheetDraftContext? context = await TimesheetDrafts.LoadAsync(id, dbContext, cancellationToken);
        if (context is null)
        {
            return TypedResults.NotFound();
        }
        if (user.EmployeeId != context.Timesheet.EmployeeId || context.Timesheet.TimesheetStatusId != TimesheetWorkflow.DraftStatusId)
        {
            return TypedResults.Forbid();
        }

        return TypedResults.Ok(Allocate(context, draft, day));
    }

    private static TimesheetAllocation Allocate(TimesheetDraftContext context, TimesheetDraft draft, int? dayNumber)
    {
        TimesheetDraftSnapshot snapshot = TimesheetDrafts.BuildSnapshot(context, draft);
        bool tracksAttendance = EmployeeTypes.TracksAttendance(context.Timesheet.Employee.EmployeeTypeId);
        IReadOnlyList<TimesheetDraftDayState> days = dayNumber.HasValue ? snapshot.Days.Where(day => day.Date.Day == dayNumber.Value).ToArray() : OrderDays(snapshot.Days);
        Dictionary<Guid, decimal> targets = snapshot.Projects.ToDictionary(project => project.Id, project => Math.Max(0m, MonthlyTarget(snapshot, project.Workload) - snapshot.Days.Sum(value => value.ProjectHours.GetValueOrDefault(project.Id))));
        decimal coreTarget = Math.Max(0m, MonthlyTarget(snapshot, context.CoreWorkload) - snapshot.Days.Sum(value => value.CoreHours));

        foreach (TimesheetDraftDayState day in days)
        {
            AllocateDay(day, snapshot.Projects, context.TotalWorkload, tracksAttendance, ref coreTarget, targets);
        }

        List<TimesheetAllocationDay> allocation = snapshot.Days.Select(day => new TimesheetAllocationDay(Date: day.Date, CoreHours: day.CoreHours, ProjectHours: day.ProjectHours)).ToList();
        return new TimesheetAllocation(Days: allocation, Evaluation: TimesheetDrafts.Evaluate(context, snapshot));
    }

    private static void AllocateDay(TimesheetDraftDayState day, IReadOnlyList<TimesheetDraftProjectState> projects, decimal totalWorkload, bool tracksAttendance, ref decimal coreTarget, Dictionary<Guid, decimal> projectTargets)
    {
        if (TimesheetInterruptions.HasBusinessTripInterruption(day.Description))
        {
            return;
        }

        if (TimesheetInterruptions.HasCoreOnlyInterruption(day.Description) || TimesheetInterruptions.HasProportionalInterruption(day.Description))
        {
            if (TimesheetInterruptions.HasCoreOnlyInterruption(day.Description))
            {
                foreach (TimesheetDraftProjectState project in projects)
                {
                    projectTargets[project.Id] += day.ProjectHours.GetValueOrDefault(project.Id);
                }
            }

            decimal previousCoreHours = day.CoreHours;
            TimesheetInterruptionHours.ApplyToDayState(day, projects, totalWorkload, tracksAttendance);
            coreTarget = Math.Max(0m, coreTarget - (day.CoreHours - previousCoreHours));
            return;
        }

        decimal capacity = TimesheetInterruptionHours.DayCapacity(day.Date, day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd, day.Description, totalWorkload, tracksAttendance);
        decimal free = TimesheetLogic.Normalize(capacity - day.CoreHours - day.ProjectHours.Values.Sum());
        if (free <= 0)
        {
            return;
        }

        decimal stagHours = TimesheetLogic.CalculateStagHours(day.Schedules);
        decimal stagMissing = Math.Max(0m, stagHours - day.CoreHours);
        bool coreCanReceiveRemainder = coreTarget > 0m;
        if (stagMissing > 0)
        {
            decimal core = PreferQuarterStagTopUp(day.CoreHours, stagHours, free, coreTarget);
            day.CoreHours += core;
            coreTarget = Math.Max(0m, coreTarget - core);
            free -= core;
        }

        List<(Guid ProjectId, decimal Remaining)> projectRemaining = [];
        foreach (TimesheetDraftProjectState project in projects)
        {
            if (!project.Locked && day.ProjectHours.GetValueOrDefault(project.Id) == 0 && projectTargets[project.Id] > 0)
            {
                projectRemaining.Add((project.Id, projectTargets[project.Id]));
            }
        }

        decimal coreRemaining = coreCanReceiveRemainder ? coreTarget : 0m;
        decimal totalRemaining = coreRemaining + projectRemaining.Sum(item => item.Remaining);
        decimal amount = Math.Min(free, totalRemaining);
        decimal left = amount;

        foreach ((Guid projectId, decimal target) in projectRemaining)
        {
            decimal maxValue = Math.Min(target, left);
            decimal value = PreferQuarter(TimesheetLogic.Normalize(amount * target / totalRemaining), maxValue);
            left -= value;
            day.ProjectHours[projectId] = value;
            projectTargets[projectId] = Math.Max(0m, projectTargets[projectId] - value);
        }

        if (coreRemaining > 0m && left > 0m)
        {
            day.CoreHours += left;
            coreTarget = Math.Max(0m, coreTarget - left);
            left = 0m;
        }

        foreach ((Guid projectId, _) in projectRemaining)
        {
            if (left <= 0m)
            {
                break;
            }

            decimal value = Math.Min(projectTargets[projectId], left);
            day.ProjectHours[projectId] += value;
            projectTargets[projectId] = Math.Max(0m, projectTargets[projectId] - value);
            left -= value;
        }
    }

    private static decimal MonthlyTarget(TimesheetDraftSnapshot snapshot, decimal workload)
    {
        int fundedDays = snapshot.Days.Count(day => TimesheetLogic.IsWeekday(day.Date));
        return TimesheetLogic.Normalize(fundedDays * 8m * workload);
    }

    private static decimal PreferQuarterStagTopUp(decimal currentCoreHours, decimal stagHours, decimal free, decimal coreTarget)
    {
        decimal required = TimesheetLogic.Normalize(stagHours - currentCoreHours);
        if (required <= 0m)
        {
            return 0m;
        }

        decimal roundedFinal = RoundUpToQuarter(stagHours);
        decimal rounded = TimesheetLogic.Normalize(Math.Max(required, roundedFinal - currentCoreHours));
        return rounded <= free && rounded <= Math.Max(required, coreTarget) ? rounded : Math.Min(required, free);
    }

    private static decimal PreferQuarter(decimal value, decimal max)
    {
        decimal rounded = RoundToQuarter(value);
        if (rounded > max)
        {
            rounded = Math.Floor(max * 4m) / 4m;
        }

        return rounded > 0m ? rounded : Math.Min(value, max);
    }

    private static decimal RoundToQuarter(decimal value) => TimesheetLogic.Normalize(Math.Round(value * 4m, MidpointRounding.AwayFromZero) / 4m);
    private static decimal RoundUpToQuarter(decimal value) => TimesheetLogic.Normalize(Math.Ceiling(value * 4m) / 4m);

    private static IReadOnlyList<TimesheetDraftDayState> OrderDays(IReadOnlyList<TimesheetDraftDayState> days) => days
        .OrderByDescending(day => !string.IsNullOrWhiteSpace(day.Description))
        .ThenByDescending(day => TimesheetLogic.CalculateStagHours(day.Schedules) > day.CoreHours)
        .ThenByDescending(day => day.ClockIn is not null && day.ClockOut is not null)
        .ThenBy(day => day.Date)
        .ToArray();
}
