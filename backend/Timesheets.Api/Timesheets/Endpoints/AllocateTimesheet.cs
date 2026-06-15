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

        decimal stagMissing = Math.Max(0m, TimesheetLogic.CalculateStagHours(day.Schedules) - day.CoreHours);
        if (stagMissing > 0 && day.CoreHours == 0)
        {
            decimal core = Math.Min(free, Math.Min(stagMissing, coreTarget));
            day.CoreHours += core;
            coreTarget -= core;
            free -= core;
        }

        List<(Guid? ProjectId, decimal Remaining)> remaining = [];
        if (day.CoreHours == 0 && coreTarget > 0)
        {
            remaining.Add((null, coreTarget));
        }

        foreach (TimesheetDraftProjectState project in projects)
        {
            if (!project.Locked && day.ProjectHours.GetValueOrDefault(project.Id) == 0 && projectTargets[project.Id] > 0)
            {
                remaining.Add((project.Id, projectTargets[project.Id]));
            }
        }

        decimal totalRemaining = remaining.Sum(item => item.Remaining);
        decimal amount = Math.Min(free, totalRemaining);
        decimal left = amount;

        for (int index = 0; index < remaining.Count; index++)
        {
            (Guid? projectId, decimal target) = remaining[index];
            decimal value = index == remaining.Count - 1 ? left : TimesheetLogic.Normalize(amount * target / totalRemaining);
            value = Math.Min(value, target);
            left -= value;

            if (projectId.HasValue)
            {
                day.ProjectHours[projectId.Value] = value;
                projectTargets[projectId.Value] -= value;
            }
            else
            {
                day.CoreHours = value;
                coreTarget -= value;
            }
        }
    }

    private static decimal MonthlyTarget(TimesheetDraftSnapshot snapshot, decimal workload)
    {
        int fundedDays = snapshot.Days.Count(day => TimesheetLogic.IsWeekday(day.Date));
        return TimesheetLogic.Normalize(fundedDays * 8m * workload);
    }

    private static IReadOnlyList<TimesheetDraftDayState> OrderDays(IReadOnlyList<TimesheetDraftDayState> days) => days
        .OrderByDescending(day => !string.IsNullOrWhiteSpace(day.Description))
        .ThenByDescending(day => day.ClockIn is not null && day.ClockOut is not null)
        .ThenBy(day => day.Date)
        .ToArray();
}
