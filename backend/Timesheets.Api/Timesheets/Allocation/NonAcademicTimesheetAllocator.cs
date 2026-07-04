namespace Timesheets.Api.Timesheets.Allocation;

/// <summary>Generates attendance-tracking (non-academic) timesheets: 6-12 h cells, attendance rebuilt from allocation.</summary>
internal sealed class NonAcademicTimesheetAllocator
{
    private readonly EditableTimesheet _sheet;
    private readonly decimal _totalWorkload;
    private readonly AttendanceGenerator _attendance = new();

    public NonAcademicTimesheetAllocator(LoadedTimesheet loaded, EditableTimesheet sheet)
    {
        _sheet = sheet;
        _totalWorkload = loaded.TotalWorkload;
    }

    public void AllocateMonth()
    {
        ResetGeneratedAllocations();
        ApplyProportionalInterruptions();
        MonthlyTargets targets = MonthlyTargets.Remainders(_sheet, _totalWorkload);
        GeneratedCellPacker packer = new(_sheet);
        foreach (ProjectColumn project in _sheet.Projects.OrderByDescending(project => targets.Project(project.Id)))
        {
            packer.Place(project.Id, targets.Project(project.Id));
        }

        packer.Place(projectId: null, targets.Core);
        RebuildAttendanceFromAllocation();
        EnsureMonthTargets();
    }

    public void AllocateDay(int dayNumber)
    {
        EditableTimesheetDay? day = _sheet.Days.SingleOrDefault(day => day.Date.Day == dayNumber);
        if (day is null)
        {
            return;
        }
        if (ApplyLockedProjectDayAttendance(day))
        {
            return;
        }

        MonthlyTargets targets = MonthlyTargets.Remainders(_sheet, _totalWorkload);
        GenerateDayAttendanceIfMissing(day);
        new DayTargetFiller(_sheet.Projects, _totalWorkload, tracksAttendance: true, targets).Fill(day);
    }

    private void ResetGeneratedAllocations()
    {
        foreach (EditableTimesheetDay day in _sheet.Days)
        {
            if (!day.CoreHoursFixed)
            {
                day.CoreHours = 0m;
            }

            foreach (ProjectColumn project in _sheet.Projects)
            {
                if (!day.ProjectHoursFixed.GetValueOrDefault(project.Id))
                {
                    day.ProjectHours[project.Id] = 0m;
                }
            }
        }
    }

    private void ApplyProportionalInterruptions()
    {
        foreach (EditableTimesheetDay day in _sheet.Days.Where(day => TimesheetInterruptions.HasProportionalInterruption(day.Description) && !day.HasLockedProjectHours()))
        {
            if (TimesheetLogic.CalculateWorkedHoursFromAttendance(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd) <= 0m)
            {
                _attendance.Set(day, TimesheetLogic.Normalize(8m * _totalWorkload));
            }

            TimesheetInterruptionHours.ApplyToDayState(day, _sheet.Projects, _totalWorkload, tracksAttendance: true);
        }
    }

    private bool ApplyLockedProjectDayAttendance(EditableTimesheetDay day)
    {
        if (!day.HasLockedProjectHours() || TimesheetInterruptions.SkipAllocationRules(day.Description))
        {
            return false;
        }

        if (!day.CoreHoursFixed)
        {
            day.CoreHours = 0m;
        }

        foreach (ProjectColumn project in _sheet.Projects)
        {
            if (!day.ProjectHoursFixed.GetValueOrDefault(project.Id))
            {
                day.ProjectHours[project.Id] = 0m;
            }
        }

        decimal work = day.TotalHours();
        if (work > 12m)
        {
            throw new InvalidOperationException($"Locked day {day.Date:yyyy-MM-dd} has {work:F2} h, expected at most 12 h.");
        }

        _attendance.Set(day, work);
        return true;
    }

    private void GenerateDayAttendanceIfMissing(EditableTimesheetDay day)
    {
        if (TimesheetInterruptions.SkipAllocationRules(day.Description) || TimesheetLogic.CalculateWorkedHoursFromAttendance(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd) > 0m)
        {
            return;
        }

        if (TimesheetLogic.CalculateStagHours(day.Schedules) > 0m || day.TotalHours() > 0m)
        {
            _attendance.GenerateIfMissing(day);
            return;
        }

        _attendance.Set(day, TimesheetLogic.Normalize(8m * _totalWorkload));
    }

    private void RebuildAttendanceFromAllocation()
    {
        foreach (EditableTimesheetDay day in _sheet.Days)
        {
            decimal work = day.TotalHours();
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

            _attendance.Set(day, work);
        }
    }

    private void EnsureMonthTargets()
    {
        List<string> errors = [];
        decimal worked = TimesheetLogic.Normalize(_sheet.Days.Sum(day => TimesheetLogic.CalculateWorkedHoursFromAttendance(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd)));
        MonthlyTargets.AppendMismatch(errors, "worked", worked, MonthlyTargets.TotalTarget(_sheet, _totalWorkload));
        MonthlyTargets.AppendMismatch(errors, "core", TimesheetLogic.Normalize(_sheet.Days.Sum(day => day.CoreHours)), MonthlyTargets.CoreTarget(_sheet, _totalWorkload));
        foreach (ProjectColumn project in _sheet.Projects)
        {
            MonthlyTargets.AppendMismatch(errors, $"project {project.Id}", TimesheetLogic.Normalize(_sheet.Days.Sum(day => day.ProjectHours.GetValueOrDefault(project.Id))), MonthlyTargets.ProjectTarget(_sheet, project));
        }

        foreach (EditableTimesheetDay day in _sheet.Days.Where(day => !TimesheetInterruptions.SkipAllocationRules(day.Description)))
        {
            decimal total = day.TotalHours();
            if (total > 0m && (total < 6m || total > 12m))
            {
                errors.Add($"day {day.Date:yyyy-MM-dd} total {total:F2}/6-12");
            }
            if (day.CoreHours > 0m && (day.CoreHours < 6m || day.CoreHours > 12m))
            {
                errors.Add($"day {day.Date:yyyy-MM-dd} core {day.CoreHours:F2}/6-12");
            }
            foreach (ProjectColumn project in _sheet.Projects)
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

    /// <summary>Bin-packs a monthly target into minute-representable 6-12 h day cells.</summary>
    private sealed class GeneratedCellPacker(EditableTimesheet sheet)
    {
        private const int MinMinutes = 6 * 60;
        private const int MaxMinutes = 12 * 60;
        private const int MinHourCents = 6 * 100;
        private const int MaxHourCents = 12 * 100;
        private static readonly decimal[] HourValues = Enumerable
            .Range(MinMinutes, MaxMinutes - MinMinutes + 1)
            .Select(MinutesToHours)
            .Distinct()
            .OrderBy(value => value)
            .ToArray();
        private static readonly HashSet<int> HourCents = HourValues.Select(ToCents).ToHashSet();

        private readonly HumanHours _humanHours = new();

        public void Place(Guid? projectId, decimal target)
        {
            target = TimesheetLogic.Normalize(target);
            if (target <= 0m)
            {
                return;
            }

            foreach (int count in CandidateCellCounts(target))
            {
                IReadOnlyList<decimal> amounts = SplitAmounts(target, count);
                List<(EditableTimesheetDay Day, decimal Amount)>? placements = TryPlanPlacements(projectId, amounts);
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

        private static IEnumerable<int> CandidateCellCounts(decimal target)
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

        private IReadOnlyList<decimal> SplitAmounts(decimal target, int count)
        {
            target = TimesheetLogic.Normalize(target);
            if (!CanSplitAmount(target, count))
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

                decimal preferred = rest == 0 ? remaining : _humanHours.RandomDayHours();
                decimal? amount = HourValues
                    .Where(value => value >= min && value <= max && CanSplitAmount(TimesheetLogic.Normalize(remaining - value), rest))
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

        private static bool CanSplitAmount(decimal total, int count)
        {
            int cents = ToCents(total);
            if (count == 0)
            {
                return cents == 0;
            }

            int min = MinHourCents * count;
            int max = MaxHourCents * count;
            if (cents < min || cents > max)
            {
                return false;
            }

            if (count == 1)
            {
                return HourCents.Contains(cents);
            }

            // Two or more minute-based 6-12 h values can compose every cent value in range except these edge cents.
            return cents != min + 1 && cents != max - 1;
        }

        private List<(EditableTimesheetDay Day, decimal Amount)>? TryPlanPlacements(Guid? projectId, IReadOnlyList<decimal> amounts)
        {
            Dictionary<EditableTimesheetDay, decimal> planned = [];
            List<(EditableTimesheetDay Day, decimal Amount)> placements = [];

            foreach (decimal amount in amounts.OrderByDescending(value => value))
            {
                EditableTimesheetDay? day = EligibleDays(projectId)
                    .Select(day => new
                    {
                        Day = day,
                        Free = TimesheetLogic.Normalize(12m - day.TotalHours() - planned.GetValueOrDefault(day))
                    })
                    .Where(candidate => candidate.Free >= amount && IsRepresentableAttendanceHours(candidate.Day.TotalHours() + planned.GetValueOrDefault(candidate.Day) + amount))
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

        private IEnumerable<EditableTimesheetDay> EligibleDays(Guid? projectId)
        {
            foreach (EditableTimesheetDay day in sheet.Days)
            {
                if (day.IsHoliday || TimesheetInterruptions.SkipAllocationRules(day.Description))
                {
                    continue;
                }
                if (day.HasLockedProjectHours())
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

        private static bool IsRepresentableAttendanceHours(decimal hours) =>
            !TimesheetLogic.HasUnequalHours(MinutesToHours(ToRoundedMinutes(hours)), hours);

        private static decimal MinutesToHours(int minutes) => TimesheetLogic.Normalize(minutes / 60m);

        private static int ToRoundedMinutes(decimal hours) => (int)Math.Round(hours * 60m, MidpointRounding.AwayFromZero);

        private static int ToCents(decimal hours) => (int)Math.Round(TimesheetLogic.Normalize(hours) * 100m, MidpointRounding.AwayFromZero);
    }
}
