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
    private const int MinGeneratedMinutes = 6 * 60;
    private const int MaxGeneratedMinutes = 12 * 60;
    private const int MinGeneratedHourCents = 6 * 100;
    private const int MaxGeneratedHourCents = 12 * 100;
    private static readonly decimal[] GeneratedAttendanceHourValues = Enumerable
        .Range(MinGeneratedMinutes, MaxGeneratedMinutes - MinGeneratedMinutes + 1)
        .Select(MinutesToHours)
        .Distinct()
        .OrderBy(value => value)
        .ToArray();
    private static readonly HashSet<int> GeneratedAttendanceHourCents = GeneratedAttendanceHourValues
        .Select(ToCents)
        .ToHashSet();

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/{id}/allocate", Handle)
            .WithSummary("Allocate Timesheet Edit")
            .WithRequestValidation<TimesheetEditRequest>();

    public sealed record DayResponse(DateTime Date, int?[] Work, int?[] Break, decimal CoreHours, IReadOnlyDictionary<Guid, decimal> ProjectHours);
    public sealed record Response(IReadOnlyList<DayResponse> Days, TimesheetEvaluation Evaluation);

    private static async Task<Results<Ok<Response>, NotFound, ForbidHttpResult>> Handle(Guid id, [FromQuery] int? day, [FromBody] TimesheetEditRequest request, AppDbContext dbContext, ICurrentUser user, CancellationToken cancellationToken)
    {
        LoadedTimesheet? loaded = await TimesheetEngine.LoadAsync(id, dbContext, cancellationToken);
        if (loaded is null)
        {
            return TypedResults.NotFound();
        }
        if ((!user.IsGlobalManagerRole() && user.EmployeeId != loaded.Timesheet.EmployeeId) || loaded.Timesheet.TimesheetStatusId != TimesheetWorkflow.DraftStatusId)
        {
            return TypedResults.Forbid();
        }

        return TypedResults.Ok(Allocate(loaded, request, day));
    }

    private static Response Allocate(LoadedTimesheet loaded, TimesheetEditRequest request, int? dayNumber)
    {
        EditableTimesheet sheet = TimesheetEngine.BuildEditableTimesheet(loaded, request);
        bool tracksAttendance = EmployeeTypes.TracksAttendance(loaded.Timesheet.Employee.EmployeeTypeId);

        if (tracksAttendance)
        {
            if (dayNumber is int day)
            {
                AllocateNonAcademicDay(loaded, sheet, day);
            }
            else
            {
                AllocateNonAcademicMonth(loaded, sheet);
            }
        }
        else
        {
            if (dayNumber is int day)
            {
                AllocateAcademicDay(loaded, sheet, day);
            }
            else
            {
                AllocateAcademicMonth(loaded, sheet);
            }
        }

        return CreateAllocationResponse(loaded, sheet);
    }

    private static void AllocateNonAcademicMonth(LoadedTimesheet loaded, EditableTimesheet sheet)
    {
        ResetGeneratedAllocations(sheet);
        ApplyNonAcademicInterruptions(sheet, loaded.TotalWorkload);
        Dictionary<Guid, decimal> projectTargets = CalculateProjectMonthlyRemainders(sheet);
        decimal coreTarget = CalculateCoreMonthlyRemainder(sheet, loaded.TotalWorkload);
        AllocateNonAcademicGeneratedHours(sheet, coreTarget, projectTargets);
        RebuildNonAcademicAttendanceFromAllocation(sheet);
        EnsureNonAcademicMonthTargets(sheet, loaded.TotalWorkload);
    }

    private static void AllocateNonAcademicDay(LoadedTimesheet loaded, EditableTimesheet sheet, int dayNumber)
    {
        EditableTimesheetDay? day = sheet.Days.SingleOrDefault(day => day.Date.Day == dayNumber);
        if (day is null)
        {
            return;
        }

        Dictionary<Guid, decimal> projectTargets = CalculateProjectMonthlyRemainders(sheet);
        decimal coreTarget = CalculateCoreMonthlyRemainder(sheet, loaded.TotalWorkload);
        GenerateNonAcademicDayAttendanceIfMissing(day, loaded.TotalWorkload);
        FillDayFromMonthlyTargets(day, sheet.Projects, loaded.TotalWorkload, tracksAttendance: true, ref coreTarget, projectTargets);
    }

    private static void AllocateAcademicDay(LoadedTimesheet loaded, EditableTimesheet sheet, int dayNumber)
    {
        EditableTimesheetDay? day = sheet.Days.SingleOrDefault(day => day.Date.Day == dayNumber);
        if (day is null)
        {
            return;
        }

        Dictionary<Guid, decimal> projectTargets = CalculateProjectMonthlyRemainders(sheet);
        decimal coreTarget = CalculateCoreMonthlyRemainder(sheet, loaded.TotalWorkload);
        FillDayFromMonthlyTargets(day, sheet.Projects, loaded.TotalWorkload, tracksAttendance: false, ref coreTarget, projectTargets);
    }

    private static Response CreateAllocationResponse(LoadedTimesheet loaded, EditableTimesheet sheet)
    {
        List<DayResponse> allocation = sheet.Days
            .Select(day => new DayResponse(
                Date: day.Date,
                Work: [ConvertToMinutes(day.ClockIn), ConvertToMinutes(day.ClockOut)],
                Break: [ConvertToMinutes(day.BreakStart), ConvertToMinutes(day.BreakEnd)],
                CoreHours: day.CoreHours,
                ProjectHours: day.ProjectHours))
            .ToList();
        return new Response(Days: allocation, Evaluation: TimesheetEngine.Evaluate(loaded, sheet));
    }

    private static void FillDayFromMonthlyTargets(EditableTimesheetDay day, IReadOnlyList<ProjectColumn> projects, decimal totalWorkload, bool tracksAttendance, ref decimal coreTarget, Dictionary<Guid, decimal> projectTargets)
    {
        if (TimesheetInterruptions.HasBusinessTripInterruption(day.Description))
        {
            return;
        }

        if (TimesheetInterruptions.HasProportionalInterruption(day.Description))
        {
            decimal previousCoreHours = day.CoreHours;
            Dictionary<Guid, decimal> previousProjectHours = projects.ToDictionary(project => project.Id, project => day.ProjectHours.GetValueOrDefault(project.Id));
            TimesheetInterruptionHours.ApplyToDayState(day, projects, totalWorkload, tracksAttendance);
            coreTarget = Math.Max(0m, coreTarget - (day.CoreHours - previousCoreHours));
            foreach (ProjectColumn project in projects)
            {
                projectTargets[project.Id] = Math.Max(0m, projectTargets[project.Id] - (day.ProjectHours.GetValueOrDefault(project.Id) - previousProjectHours[project.Id]));
            }

            return;
        }

        decimal capacity = TimesheetInterruptionHours.DayCapacity(day.Date, day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd, day.Description, totalWorkload, tracksAttendance, day.Schedules);
        decimal free = TimesheetLogic.Normalize(capacity - day.CoreHours - day.ProjectHours.Values.Sum());
        if (free <= 0)
        {
            return;
        }

        decimal stagHours = TimesheetLogic.CalculateStagHours(day.Schedules);
        decimal stagMissing = Math.Max(0m, stagHours - day.CoreHours);
        bool coreCanReceiveRemainder = !day.CoreHoursFixed && coreTarget > 0m;
        if (!day.CoreHoursFixed && stagMissing > 0)
        {
            decimal core = PreferStagTopUpHours(day.CoreHours, stagHours, free, coreTarget);
            day.CoreHours += core;
            coreTarget = Math.Max(0m, coreTarget - core);
            free -= core;
        }

        List<(Guid ProjectId, decimal Remaining)> projectRemaining = [];
        foreach (ProjectColumn project in projects)
        {
            bool fixedHours = day.ProjectHoursFixed.GetValueOrDefault(project.Id);
            if (project.IsActiveOn(day.Date) && !project.Locked && !fixedHours && day.ProjectHours.GetValueOrDefault(project.Id) == 0 && projectTargets[project.Id] > 0)
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
            decimal value = PreferGeneratedCellHours(TimesheetLogic.Normalize(amount * target / totalRemaining), maxValue);
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

    private static void AllocateAcademicMonth(LoadedTimesheet loaded, EditableTimesheet sheet)
    {
        foreach (EditableTimesheetDay day in sheet.Days.Where(day => TimesheetInterruptions.SkipAllocationRules(day.Description)))
        {
            TimesheetInterruptionHours.ApplyToDayState(day, sheet.Projects, loaded.TotalWorkload, tracksAttendance: false);
        }

        foreach (EditableTimesheetDay day in sheet.Days.Where(day => !day.CoreHoursFixed && !TimesheetInterruptions.SkipAllocationRules(day.Description)))
        {
            decimal stagMissing = TimesheetLogic.Normalize(TimesheetLogic.CalculateStagHours(day.Schedules) - day.CoreHours);
            if (stagMissing > 0m)
            {
                day.CoreHours = TimesheetLogic.Normalize(day.CoreHours + stagMissing);
            }
        }

        decimal coreTarget = CalculateCoreMonthlyRemainder(sheet, loaded.TotalWorkload);
        Dictionary<Guid, decimal> projectTargets = CalculateProjectMonthlyRemainders(sheet);
        decimal remaining = TimesheetLogic.Normalize(coreTarget + projectTargets.Values.Sum());
        if (remaining <= 0m)
        {
            return;
        }

        List<EditableTimesheetDay> candidates = sheet.Days
            .Where(day =>
                CanAllocateAcademicDay(day) &&
                !TimesheetInterruptions.SkipAllocationRules(day.Description) &&
                CanDayReceiveHours(day, sheet.Projects))
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        List<EditableTimesheetDay> activeDays = candidates.Where(day => CalculateDayTotal(day) > 0m).ToList();
        int activeCount = ChooseActiveDayCount(candidates.Count, activeDays.Count, candidates.Sum(CalculateDayTotal) + remaining);
        foreach (EditableTimesheetDay day in candidates
            .Where(day => CalculateDayTotal(day) == 0m)
            .OrderBy(_ => Random.Shared.Next())
            .Take(Math.Max(0, activeCount - activeDays.Count)))
        {
            activeDays.Add(day);
        }

        decimal neededCapacity = TimesheetLogic.Normalize(activeDays.Sum(CalculateDayTotal) + remaining);
        foreach (EditableTimesheetDay day in candidates.Except(activeDays).OrderBy(_ => Random.Shared.Next()))
        {
            if (activeDays.Sum(CalculateAcademicDayMaxHours) >= neededCapacity)
            {
                break;
            }

            activeDays.Add(day);
        }

        Dictionary<EditableTimesheetDay, decimal> dayTargets = activeDays.ToDictionary(day => day, CalculateDayTotal);
        decimal remainingForDayTargets = remaining;
        decimal finalTotal = TimesheetLogic.Normalize(dayTargets.Values.Sum() + remainingForDayTargets);
        if (finalTotal >= 6m)
        {
            foreach (EditableTimesheetDay day in activeDays.OrderBy(_ => Random.Shared.Next()))
            {
                decimal dayMax = CalculateAcademicDayMaxHours(day);
                decimal add = Math.Min(Math.Min(6m - dayTargets[day], dayMax - dayTargets[day]), remainingForDayTargets);
                if (add > 0m)
                {
                    dayTargets[day] = TimesheetLogic.Normalize(dayTargets[day] + add);
                    remainingForDayTargets = TimesheetLogic.Normalize(remainingForDayTargets - add);
                }
            }
        }

        foreach (EditableTimesheetDay day in activeDays.OrderBy(_ => Random.Shared.Next()))
        {
            decimal dayMax = CalculateAcademicDayMaxHours(day);
            decimal add = Math.Min(Math.Min(GenerateRandomDayHours() - dayTargets[day], dayMax - dayTargets[day]), remainingForDayTargets);
            if (add > 0m)
            {
                dayTargets[day] = TimesheetLogic.Normalize(dayTargets[day] + add);
                remainingForDayTargets = TimesheetLogic.Normalize(remainingForDayTargets - add);
            }
        }

        while (remainingForDayTargets > 0m)
        {
            List<EditableTimesheetDay> available = activeDays.Where(day => dayTargets[day] < CalculateAcademicDayMaxHours(day)).ToList();
            if (available.Count == 0)
            {
                break;
            }

            EditableTimesheetDay day = available[Random.Shared.Next(available.Count)];
            decimal dayMax = CalculateAcademicDayMaxHours(day);
            decimal add = Math.Min(Math.Min(GenerateRandomAmount(dayMax - dayTargets[day]), dayMax - dayTargets[day]), remainingForDayTargets);
            dayTargets[day] = TimesheetLogic.Normalize(dayTargets[day] + add);
            remainingForDayTargets = TimesheetLogic.Normalize(remainingForDayTargets - add);
        }

        Dictionary<Guid, ProjectColumn> projectsById = sheet.Projects.ToDictionary(project => project.Id);
        while (coreTarget + projectTargets.Values.Sum() > 0m)
        {
            List<(EditableTimesheetDay Day, Guid? ProjectId, decimal Gap, decimal Remaining)> options = [];
            foreach ((EditableTimesheetDay day, decimal target) in dayTargets)
            {
                decimal gap = TimesheetLogic.Normalize(target - CalculateDayTotal(day));
                if (gap <= 0m)
                {
                    continue;
                }

                if (!day.CoreHoursFixed && coreTarget > 0m)
                {
                    options.Add((day, null, gap, coreTarget));
                }

                foreach ((Guid projectId, decimal targetLeft) in projectTargets)
                {
                    if (targetLeft > 0m &&
                        projectsById[projectId].IsActiveOn(day.Date) &&
                        !projectsById[projectId].Locked &&
                        !day.ProjectHoursFixed.GetValueOrDefault(projectId))
                    {
                        options.Add((day, projectId, gap, targetLeft));
                    }
                }
            }

            if (options.Count == 0)
            {
                break;
            }

            List<(EditableTimesheetDay Day, Guid? ProjectId, decimal Gap, decimal Remaining)> nonTinyOptions = options
                .Where(option => option.Gap >= 1m && option.Remaining >= 1m)
                .ToList();
            if (nonTinyOptions.Count > 0)
            {
                options = nonTinyOptions;
            }
            else
            {
                break;
            }

            (EditableTimesheetDay selectedDay, Guid? selectedProjectId, decimal selectedGap, decimal selectedTargetLeft) =
                options[Random.Shared.Next(options.Count)];
            decimal amount = Math.Min(Math.Min(GenerateRandomAmount(selectedGap), selectedGap), selectedTargetLeft);
            if (selectedProjectId is null)
            {
                selectedDay.CoreHours = TimesheetLogic.Normalize(selectedDay.CoreHours + amount);
                coreTarget = TimesheetLogic.Normalize(coreTarget - amount);
            }
            else
            {
                selectedDay.ProjectHours[selectedProjectId.Value] = TimesheetLogic.Normalize(selectedDay.ProjectHours.GetValueOrDefault(selectedProjectId.Value) + amount);
                projectTargets[selectedProjectId.Value] = TimesheetLogic.Normalize(projectTargets[selectedProjectId.Value] - amount);
            }
        }

        CompleteMonthlyTargets(activeDays, sheet.Projects, CalculateFreeAcademicHours, ref coreTarget, projectTargets);
    }

    private static bool CanAllocateAcademicDay(EditableTimesheetDay day) =>
        TimesheetLogic.IsWeekday(day.Date) || TimesheetLogic.CalculateStagHours(day.Schedules) > 0m;

    private static decimal CalculateAcademicDayMaxHours(EditableTimesheetDay day)
    {
        if (TimesheetLogic.IsWeekday(day.Date) || !string.IsNullOrWhiteSpace(day.Description))
        {
            return 12m;
        }

        decimal stagHours = TimesheetLogic.CalculateStagHours(day.Schedules);
        return stagHours > 0m ? TimesheetLogic.Normalize(Math.Min(12m, stagHours)) : 0m;
    }

    private static int ChooseActiveDayCount(int candidatesCount, int activeCount, decimal totalHours)
    {
        int lower = Math.Max(activeCount, (int)Math.Ceiling(totalHours / 12m));
        int upper = Math.Min(candidatesCount, (int)Math.Floor(totalHours / 6m));
        if (activeCount < candidatesCount && totalHours <= 12m * (candidatesCount - 1))
        {
            upper = Math.Min(upper, candidatesCount - 1);
        }

        if (upper < lower)
        {
            upper = lower;
        }

        int preferred = (int)Math.Round(totalHours / GenerateRandomDayHours(), MidpointRounding.AwayFromZero) + Random.Shared.Next(-2, 3);
        return Math.Min(candidatesCount, Math.Clamp(preferred, lower, upper));
    }

    private static bool CanDayReceiveHours(EditableTimesheetDay day, IReadOnlyList<ProjectColumn> projects) =>
        !day.CoreHoursFixed || projects.Any(project => project.IsActiveOn(day.Date) && !project.Locked && !day.ProjectHoursFixed.GetValueOrDefault(project.Id));

    private static decimal CalculateDayTotal(EditableTimesheetDay day) => TimesheetLogic.Normalize(day.CoreHours + day.ProjectHours.Values.Sum());

    private static void ResetGeneratedAllocations(EditableTimesheet sheet)
    {
        foreach (EditableTimesheetDay day in sheet.Days)
        {
            if (!day.CoreHoursFixed)
            {
                day.CoreHours = 0m;
            }

            foreach (ProjectColumn project in sheet.Projects)
            {
                if (!day.ProjectHoursFixed.GetValueOrDefault(project.Id))
                {
                    day.ProjectHours[project.Id] = 0m;
                }
            }
        }
    }

    private static void ApplyNonAcademicInterruptions(EditableTimesheet sheet, decimal totalWorkload)
    {
        foreach (EditableTimesheetDay day in sheet.Days.Where(day => TimesheetInterruptions.HasProportionalInterruption(day.Description)))
        {
            if (TimesheetLogic.CalculateWorkedHoursFromAttendance(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd) <= 0m)
            {
                SetGeneratedAttendance(day, TimesheetLogic.Normalize(8m * totalWorkload));
            }

            TimesheetInterruptionHours.ApplyToDayState(day, sheet.Projects, totalWorkload, tracksAttendance: true);
        }
    }

    private static void AllocateNonAcademicGeneratedHours(EditableTimesheet sheet, decimal coreTarget, Dictionary<Guid, decimal> projectTargets)
    {
        foreach (ProjectColumn project in sheet.Projects.OrderByDescending(project => projectTargets[project.Id]))
        {
            PlaceGeneratedHours(sheet, project.Id, projectTargets[project.Id]);
        }

        PlaceGeneratedHours(sheet, projectId: null, coreTarget);
    }

    private static void PlaceGeneratedHours(EditableTimesheet sheet, Guid? projectId, decimal target)
    {
        target = TimesheetLogic.Normalize(target);
        if (target <= 0m)
        {
            return;
        }

        foreach (int count in CandidateGeneratedCellCounts(target))
        {
            IReadOnlyList<decimal> amounts = SplitGeneratedAmounts(target, count);
            List<(EditableTimesheetDay Day, decimal Amount)>? placements = TryPlanGeneratedPlacements(sheet, projectId, amounts);
            if (placements is null)
            {
                continue;
            }

            foreach ((EditableTimesheetDay day, decimal amount) in placements)
            {
                if (projectId is Guid id)
                {
                    day.ProjectHours[id] = TimesheetLogic.Normalize(day.ProjectHours.GetValueOrDefault(id) + amount);
                }
                else
                {
                    day.CoreHours = TimesheetLogic.Normalize(day.CoreHours + amount);
                }
            }

            return;
        }

        throw new InvalidOperationException($"Cannot place generated hours {target:F2} for {(projectId.HasValue ? $"project {projectId}" : "core")} into 6-12 h cells.");
    }

    private static IEnumerable<int> CandidateGeneratedCellCounts(decimal target)
    {
        int min = (int)Math.Ceiling(target / 12m);
        int max = (int)Math.Floor(target / 6m);
        if (max < min)
        {
            throw new InvalidOperationException($"Cannot split generated target {target:F2} into 6-12 h cells.");
        }

        int preferred = Math.Clamp((int)Math.Round(target / 8m, MidpointRounding.AwayFromZero), min, max);
        yield return preferred;

        for (int offset = 1; preferred - offset >= min || preferred + offset <= max; offset++)
        {
            if (preferred + offset <= max)
            {
                yield return preferred + offset;
            }
            if (preferred - offset >= min)
            {
                yield return preferred - offset;
            }
        }
    }

    private static IReadOnlyList<decimal> SplitGeneratedAmounts(decimal target, int count)
    {
        target = TimesheetLogic.Normalize(target);
        if (!CanSplitGeneratedAmount(target, count))
        {
            return [];
        }

        List<decimal> amounts = [];
        decimal remaining = target;
        for (int index = 0; index < count; index++)
        {
            int rest = count - index - 1;
            decimal min = Math.Max(6m, TimesheetLogic.Normalize(remaining - 12m * rest));
            decimal max = Math.Min(12m, TimesheetLogic.Normalize(remaining - 6m * rest));
            if (min > max)
            {
                return [];
            }

            decimal preferred = rest == 0 ? remaining : GenerateRandomDayHours();
            decimal? amount = GeneratedAttendanceHourValues
                .Where(value => value >= min && value <= max && CanSplitGeneratedAmount(TimesheetLogic.Normalize(remaining - value), rest))
                .OrderBy(value => Math.Abs(value - preferred))
                .ThenBy(_ => Random.Shared.Next())
                .Cast<decimal?>()
                .FirstOrDefault();
            if (amount is null)
            {
                return [];
            }

            amounts.Add(amount.Value);
            remaining = TimesheetLogic.Normalize(remaining - amount.Value);
        }

        return remaining == 0m ? amounts : [];
    }

    private static bool CanSplitGeneratedAmount(decimal total, int count)
    {
        int cents = ToCents(total);
        if (count == 0)
        {
            return cents == 0;
        }

        int min = MinGeneratedHourCents * count;
        int max = MaxGeneratedHourCents * count;
        if (cents < min || cents > max)
        {
            return false;
        }

        if (count == 1)
        {
            return GeneratedAttendanceHourCents.Contains(cents);
        }

        // Two or more minute-based 6-12 h values can compose every cent value in range except these edge cents.
        return cents != min + 1 && cents != max - 1;
    }

    private static bool IsRepresentableAttendanceHours(decimal hours) =>
        !TimesheetLogic.HasUnequalHours(MinutesToHours(ToRoundedMinutes(hours)), hours);

    private static decimal MinutesToHours(int minutes) => TimesheetLogic.Normalize(minutes / 60m);

    private static int ToRoundedMinutes(decimal hours) => (int)Math.Round(hours * 60m, MidpointRounding.AwayFromZero);

    private static int ToCents(decimal hours) => (int)Math.Round(TimesheetLogic.Normalize(hours) * 100m, MidpointRounding.AwayFromZero);

    private static List<(EditableTimesheetDay Day, decimal Amount)>? TryPlanGeneratedPlacements(EditableTimesheet sheet, Guid? projectId, IReadOnlyList<decimal> amounts)
    {
        Dictionary<EditableTimesheetDay, decimal> planned = [];
        List<(EditableTimesheetDay Day, decimal Amount)> placements = [];

        foreach (decimal amount in amounts.OrderByDescending(value => value))
        {
            EditableTimesheetDay? day = EligibleGeneratedDays(sheet, projectId)
                .Select(day => new
                {
                    Day = day,
                    Free = TimesheetLogic.Normalize(12m - CalculateDayTotal(day) - planned.GetValueOrDefault(day))
                })
                .Where(candidate => candidate.Free >= amount && IsRepresentableAttendanceHours(CalculateDayTotal(candidate.Day) + planned.GetValueOrDefault(candidate.Day) + amount))
                .OrderBy(candidate => TimesheetLogic.Normalize(candidate.Free - amount))
                .ThenBy(candidate => TimesheetLogic.IsWeekday(candidate.Day.Date) ? 0 : 1)
                .ThenBy(_ => Random.Shared.Next())
                .Select(candidate => candidate.Day)
                .FirstOrDefault();

            if (day is null)
            {
                return null;
            }

            planned[day] = TimesheetLogic.Normalize(planned.GetValueOrDefault(day) + amount);
            placements.Add((day, amount));
        }

        return placements;
    }

    private static IEnumerable<EditableTimesheetDay> EligibleGeneratedDays(EditableTimesheet sheet, Guid? projectId)
    {
        foreach (EditableTimesheetDay day in sheet.Days)
        {
            if (day.IsHoliday || TimesheetInterruptions.SkipAllocationRules(day.Description))
            {
                continue;
            }

            if (projectId is null)
            {
                if (!day.CoreHoursFixed)
                {
                    yield return day;
                }
                continue;
            }

            ProjectColumn project = sheet.Projects.Single(project => project.Id == projectId.Value);
            if (project.IsActiveOn(day.Date) && !project.Locked && !day.ProjectHoursFixed.GetValueOrDefault(project.Id))
            {
                yield return day;
            }
        }
    }

    private static void RebuildNonAcademicAttendanceFromAllocation(EditableTimesheet sheet)
    {
        foreach (EditableTimesheetDay day in sheet.Days)
        {
            decimal work = CalculateDayTotal(day);
            if (work <= 0m)
            {
                day.ClockIn = null;
                day.ClockOut = null;
                day.BreakStart = null;
                day.BreakEnd = null;
                continue;
            }

            if (work > 12m)
            {
                throw new InvalidOperationException($"Generated day {day.Date:yyyy-MM-dd} has {work:F2} h, expected at most 12 h.");
            }

            SetGeneratedAttendance(day, work);
        }
    }

    private static void EnsureNonAcademicMonthTargets(EditableTimesheet sheet, decimal totalWorkload)
    {
        List<string> errors = [];
        decimal worked = TimesheetLogic.Normalize(sheet.Days.Sum(day => TimesheetLogic.CalculateWorkedHoursFromAttendance(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd)));
        decimal target = CalculateTotalMonthlyTarget(sheet, totalWorkload);
        decimal core = TimesheetLogic.Normalize(sheet.Days.Sum(day => day.CoreHours));
        decimal coreTarget = CalculateCoreMonthlyTarget(sheet, totalWorkload);

        AddMismatch(errors, "worked", worked, target);
        AddMismatch(errors, "core", core, coreTarget);
        foreach (ProjectColumn project in sheet.Projects)
        {
            AddMismatch(errors, $"project {project.Id}", TimesheetLogic.Normalize(sheet.Days.Sum(day => day.ProjectHours.GetValueOrDefault(project.Id))), CalculateProjectMonthlyTarget(sheet, project));
        }

        foreach (EditableTimesheetDay day in sheet.Days.Where(day => !TimesheetInterruptions.SkipAllocationRules(day.Description)))
        {
            decimal total = CalculateDayTotal(day);
            if (total > 0m && (total < 6m || total > 12m))
            {
                errors.Add($"day {day.Date:yyyy-MM-dd} total {total:F2}/6-12");
            }
            if (day.CoreHours > 0m && (day.CoreHours < 6m || day.CoreHours > 12m))
            {
                errors.Add($"day {day.Date:yyyy-MM-dd} core {day.CoreHours:F2}/6-12");
            }
            foreach (ProjectColumn project in sheet.Projects)
            {
                decimal hours = day.ProjectHours.GetValueOrDefault(project.Id);
                if (hours > 0m && (hours < 6m || hours > 12m))
                {
                    errors.Add($"day {day.Date:yyyy-MM-dd} project {project.Id} {hours:F2}/6-12");
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException("Generated non-academic timesheet missed targets: " + string.Join("; ", errors));
        }
    }

    private static void AddMismatch(List<string> errors, string label, decimal actual, decimal expected)
    {
        if (TimesheetLogic.HasUnequalHours(actual, expected))
        {
            errors.Add($"{label} {actual:F2}/{expected:F2}");
        }
    }

    private static void GenerateNonAcademicDayAttendanceIfMissing(EditableTimesheetDay day, decimal totalWorkload)
    {
        if (TimesheetInterruptions.SkipAllocationRules(day.Description) || TimesheetLogic.CalculateWorkedHoursFromAttendance(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd) > 0m)
        {
            return;
        }

        if (TimesheetLogic.CalculateStagHours(day.Schedules) > 0m || CalculateDayTotal(day) > 0m)
        {
            GenerateAttendanceIfMissing(day);
            return;
        }

        SetGeneratedAttendance(day, TimesheetLogic.Normalize(8m * totalWorkload));
    }

    private static void SetGeneratedAttendance(EditableTimesheetDay day, decimal work)
    {
        TimeSpan start = new(8, 0, 0);
        bool needsBreak = work > 6m;
        int breakMinutes = needsBreak ? 30 : 0;
        day.ClockIn = start;
        day.ClockOut = start.Add(TimeSpan.FromMinutes((double)(work * 60m) + breakMinutes));
        if (needsBreak)
        {
            TimeSpan breakStart = start.Add(TimeSpan.FromHours(4));
            day.BreakStart = breakStart;
            day.BreakEnd = breakStart.Add(TimeSpan.FromMinutes(30));
        }
        else
        {
            day.BreakStart = null;
            day.BreakEnd = null;
        }
    }
    private static void CompleteMonthlyTargets(IReadOnlyList<EditableTimesheetDay> days, IReadOnlyList<ProjectColumn> projects, Func<EditableTimesheetDay, decimal> calculateFreeHours, ref decimal coreTarget, Dictionary<Guid, decimal> projectTargets)
    {
        foreach (ProjectColumn project in projects.OrderBy(_ => Random.Shared.Next()))
        {
            decimal target = projectTargets[project.Id];
            CompleteProjectTarget(days, project, calculateFreeHours, onlyExistingCells: true, allowTinyNewCell: true, ref target);
            CompleteProjectTarget(days, project, calculateFreeHours, onlyExistingCells: false, allowTinyNewCell: false, ref target);
            CompleteProjectTarget(days, project, calculateFreeHours, onlyExistingCells: true, allowTinyNewCell: true, ref target);
            projectTargets[project.Id] = target;
        }

        CompleteCoreTarget(days, calculateFreeHours, onlyExistingCells: true, allowTinyNewCell: true, ref coreTarget);
        CompleteCoreTarget(days, calculateFreeHours, onlyExistingCells: false, allowTinyNewCell: false, ref coreTarget);
        CompleteCoreTarget(days, calculateFreeHours, onlyExistingCells: true, allowTinyNewCell: true, ref coreTarget);
    }

    private static void CompleteProjectTarget(IReadOnlyList<EditableTimesheetDay> days, ProjectColumn project, Func<EditableTimesheetDay, decimal> calculateFreeHours, bool onlyExistingCells, bool allowTinyNewCell, ref decimal target)
    {
        foreach (EditableTimesheetDay day in days.OrderBy(_ => Random.Shared.Next()))
        {
            if (target <= 0m)
            {
                return;
            }
            if (!project.IsActiveOn(day.Date) || project.Locked || day.ProjectHoursFixed.GetValueOrDefault(project.Id))
            {
                continue;
            }

            decimal current = day.ProjectHours.GetValueOrDefault(project.Id);
            if (onlyExistingCells && current <= 0m)
            {
                continue;
            }

            decimal add = CalculateCompletionAmount(target, calculateFreeHours(day), current, allowTinyNewCell);
            if (add <= 0m)
            {
                continue;
            }

            day.ProjectHours[project.Id] = TimesheetLogic.Normalize(current + add);
            target = TimesheetLogic.Normalize(target - add);
        }
    }

    private static void CompleteCoreTarget(IReadOnlyList<EditableTimesheetDay> days, Func<EditableTimesheetDay, decimal> calculateFreeHours, bool onlyExistingCells, bool allowTinyNewCell, ref decimal target)
    {
        foreach (EditableTimesheetDay day in days.OrderBy(_ => Random.Shared.Next()))
        {
            if (target <= 0m)
            {
                return;
            }
            if (day.CoreHoursFixed)
            {
                continue;
            }
            if (onlyExistingCells && day.CoreHours <= 0m)
            {
                continue;
            }

            decimal add = CalculateCompletionAmount(target, calculateFreeHours(day), day.CoreHours, allowTinyNewCell);
            if (add <= 0m)
            {
                continue;
            }

            day.CoreHours = TimesheetLogic.Normalize(day.CoreHours + add);
            target = TimesheetLogic.Normalize(target - add);
        }
    }

    private static decimal CalculateCompletionAmount(decimal target, decimal free, decimal currentCell, bool allowTinyNewCell)
    {
        decimal amount = TimesheetLogic.Normalize(Math.Min(target, free));
        if (amount <= 0m)
        {
            return 0m;
        }
        if (currentCell > 0m || amount >= 1m || allowTinyNewCell)
        {
            return amount;
        }

        return 0m;
    }

    private static decimal CalculateFreeAcademicHours(EditableTimesheetDay day) =>
        TimesheetLogic.Normalize(CalculateAcademicDayMaxHours(day) - CalculateDayTotal(day));

    private static decimal CalculateFreeNonAcademicHours(EditableTimesheetDay day, decimal totalWorkload) =>
        TimesheetLogic.Normalize(TimesheetInterruptionHours.DayCapacity(day.Date, day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd, day.Description, totalWorkload, tracksAttendance: true, day.Schedules) - CalculateDayTotal(day));

    private static decimal GenerateRandomDayHours()
    {
        decimal raw = Random.Shared.NextDouble() < 0.7
            ? 7m + (decimal)Random.Shared.NextDouble() * 2m
            : 6m + (decimal)Random.Shared.NextDouble() * 6m;
        return Math.Min(12m, Math.Max(6m, HumanizeAmount(raw)));
    }

    private static decimal GenerateRandomAmount(decimal max)
    {
        max = TimesheetLogic.Normalize(max);
        if (max <= 1m)
        {
            return TimesheetLogic.Normalize(max);
        }

        decimal raw = 1m + (decimal)Random.Shared.NextDouble() * (max - 1m);
        decimal amount = Math.Min(max, HumanizeAmount(raw));
        return max - amount is > 0m and < 1m ? max : amount;
    }

    private static decimal HumanizeAmount(decimal raw)
    {
        double mode = Random.Shared.NextDouble();
        if (mode < 0.8)
        {
            return RoundToHalfHour(raw);
        }

        if (mode < 0.95)
        {
            return RoundToQuarter(raw);
        }

        if (mode < 0.99)
        {
            return TimesheetLogic.Normalize(Math.Round(raw * 10m, MidpointRounding.AwayFromZero) / 10m);
        }

        return TimesheetLogic.Normalize(raw);
    }

    private static void GenerateAttendanceIfMissing(EditableTimesheetDay day)
    {
        if (TimesheetInterruptions.SkipAllocationRules(day.Description)
            || day.ClockIn is not null
            || day.ClockOut is not null
            || day.BreakStart is not null
            || day.BreakEnd is not null)
        {
            return;
        }

        decimal allocated = TimesheetLogic.Normalize(day.CoreHours + day.ProjectHours.Values.Sum());
        decimal stag = TimesheetLogic.CalculateStagHours(day.Schedules);
        decimal work = Math.Max(allocated, stag);
        if (work <= 0m)
        {
            return;
        }

        TimeSpan start = day.Schedules.Count > 0 ? day.Schedules.Min(schedule => schedule.Start) : new TimeSpan(7, 0, 0);
        int workMinutes = (int)Math.Round(work * 60m, MidpointRounding.AwayFromZero);
        bool needsBreak = work > 6m;
        int breakMinutes = needsBreak ? 30 : 0;
        TimeSpan end = start.Add(TimeSpan.FromMinutes(workMinutes + breakMinutes));
        if (end >= TimeSpan.FromDays(1))
        {
            return;
        }

        day.ClockIn = start;
        day.ClockOut = end;
        if (needsBreak)
        {
            day.BreakStart = start.Add(TimeSpan.FromHours(4));
            day.BreakEnd = day.BreakStart.Value.Add(TimeSpan.FromMinutes(30));
        }
    }

    private static int? ConvertToMinutes(TimeSpan? value) => value.HasValue ? (int)Math.Round(value.Value.TotalMinutes) : null;

    private static Dictionary<Guid, decimal> CalculateProjectMonthlyRemainders(EditableTimesheet sheet) =>
        sheet.Projects.ToDictionary(
            project => project.Id,
            project => TimesheetLogic.Normalize(Math.Max(0m, CalculateProjectMonthlyTarget(sheet, project) - sheet.Days.Sum(day => day.ProjectHours.GetValueOrDefault(project.Id)))));

    private static decimal CalculateTotalMonthlyTarget(EditableTimesheet sheet, decimal totalWorkload) =>
        TimesheetLogic.Normalize(CalculateCoreMonthlyTarget(sheet, totalWorkload) + sheet.Projects.Sum(project => CalculateProjectMonthlyTarget(sheet, project)));

    private static decimal CalculateCoreMonthlyRemainder(EditableTimesheet sheet, decimal totalWorkload) =>
        TimesheetLogic.Normalize(Math.Max(0m, CalculateCoreMonthlyTarget(sheet, totalWorkload) - sheet.Days.Sum(day => day.CoreHours)));

    private static decimal CalculateProjectMonthlyTarget(EditableTimesheet sheet, ProjectColumn project)
    {
        int fundedDays = sheet.Days.Count(day => TimesheetLogic.IsWeekday(day.Date) && project.IsActiveOn(day.Date));
        return TimesheetLogic.Normalize(fundedDays * 8m * project.Workload);
    }

    private static decimal CalculateCoreMonthlyTarget(EditableTimesheet sheet, decimal totalWorkload)
    {
        int fundedDays = sheet.Days.Count(day => TimesheetLogic.IsWeekday(day.Date));
        decimal total = TimesheetLogic.Normalize(fundedDays * 8m * totalWorkload);
        return TimesheetLogic.Normalize(total - sheet.Projects.Sum(project => CalculateProjectMonthlyTarget(sheet, project)));
    }

    private static decimal PreferStagTopUpHours(decimal currentCoreHours, decimal stagHours, decimal free, decimal coreTarget)
    {
        decimal required = TimesheetLogic.Normalize(stagHours - currentCoreHours);
        if (required <= 0m)
        {
            return 0m;
        }

        decimal roundedFinal = RoundUpToHalfHour(stagHours);
        decimal rounded = TimesheetLogic.Normalize(Math.Max(required, roundedFinal - currentCoreHours));
        return rounded <= free && rounded <= Math.Max(required, coreTarget) ? rounded : Math.Min(required, free);
    }

    private static decimal PreferGeneratedCellHours(decimal value, decimal max)
    {
        max = TimesheetLogic.Normalize(max);
        if (max < 1m)
        {
            return 0m;
        }
        if (max == 1m)
        {
            return max;
        }

        decimal rounded = Math.Max(1m, RoundToHalfHour(value));
        if (rounded > max)
        {
            rounded = Math.Floor(max * 2m) / 2m;
        }

        return max - rounded is > 0m and < 1m ? max : rounded;
    }

    private static decimal RoundToHalfHour(decimal value) => TimesheetLogic.Normalize(Math.Round(value * 2m, MidpointRounding.AwayFromZero) / 2m);
    private static decimal RoundToQuarter(decimal value) => TimesheetLogic.Normalize(Math.Round(value * 4m, MidpointRounding.AwayFromZero) / 4m);
    private static decimal RoundUpToHalfHour(decimal value) => TimesheetLogic.Normalize(Math.Ceiling(value * 2m) / 2m);

    private static IReadOnlyList<EditableTimesheetDay> OrderNonAcademicDays(IReadOnlyList<EditableTimesheetDay> days) => days
        .OrderByDescending(day => !string.IsNullOrWhiteSpace(day.Description))
        .ThenByDescending(day => TimesheetLogic.CalculateStagHours(day.Schedules) > day.CoreHours)
        .ThenByDescending(day => day.ClockIn is not null && day.ClockOut is not null)
        .ThenBy(day => day.Date)
        .ToArray();
}
