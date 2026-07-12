using Timesheets.Api.Features.Employees;

namespace Timesheets.Api.Features.Timesheets.Allocation;

public sealed class TimesheetAllocator
{
    public void AllocateMonth(LoadedTimesheet loaded, EditableTimesheet sheet)
    {
        if (EmployeeTypes.TracksAttendance(loaded.Attendance.EmployeeTypeId))
        {
            new NonAcademicAllocation(loaded, sheet).AllocateMonth();
        }
        else
        {
            new AcademicAllocation(loaded, sheet).AllocateMonth();
        }
    }

    public void AllocateDay(LoadedTimesheet loaded, EditableTimesheet sheet, int dayNumber)
    {
        if (EmployeeTypes.TracksAttendance(loaded.Attendance.EmployeeTypeId))
        {
            new NonAcademicAllocation(loaded, sheet).AllocateDay(dayNumber);
        }
        else
        {
            new AcademicAllocation(loaded, sheet).AllocateDay(dayNumber);
        }
    }
}

internal static class AllocationDayExtensions
{
    public const decimal CoreToleranceHours = 2m;

    public static decimal TotalHours(this EditableTimesheetDay day) => TimesheetEvaluator.Normalize(day.CoreHours + day.ContractPartHours.Values.Sum());

    public static bool HasLockedContractPartHours(this EditableTimesheetDay day) =>
        day.ContractPartHoursFixed.Any(item => item.Value && day.ContractPartHours.GetValueOrDefault(item.Key) > 0m);

    public static void ResetGeneratedAllocations(this EditableTimesheetDay day, IReadOnlyList<ContractPartColumn> projects)
    {
        if (!day.CoreHoursFixed)
        {
            day.CoreHours = 0m;
        }

        foreach (ContractPartColumn project in projects)
        {
            if (!day.ContractPartHoursFixed.GetValueOrDefault(project.Id))
            {
                day.ContractPartHours[project.Id] = day.ProjectFloor(project.Id);
            }
        }
    }

    public static decimal ProjectFloor(this EditableTimesheetDay day, Guid contractEmployeeId) =>
        day.ContractPartHoursFloor.GetValueOrDefault(contractEmployeeId);

    public static void SetContractPartHours(this EditableTimesheetDay day, Guid contractEmployeeId, decimal hours)
    {
        decimal floor = day.ProjectFloor(contractEmployeeId);
        day.ContractPartHours[contractEmployeeId] = HumanHours.RoundToHalfHour(Math.Max(floor, hours));
    }

    public static void AddContractPartHours(this EditableTimesheetDay day, Guid contractEmployeeId, decimal amount)
    {
        decimal current = day.ContractPartHours.GetValueOrDefault(contractEmployeeId);
        day.SetContractPartHours(contractEmployeeId, current + amount);
    }

    public static decimal WorkedHours(this EditableTimesheetDay day) =>
        TimesheetEvaluator.CalculateWorkedHoursFromAttendance(day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd);
}

internal sealed class MonthlyTargets
{
    private decimal _core;
    private readonly Dictionary<Guid, decimal> _projects;

    private MonthlyTargets(decimal core, Dictionary<Guid, decimal> projects)
    {
        _core = core;
        _projects = projects;
    }

    public decimal Core => _core;
    public IEnumerable<KeyValuePair<Guid, decimal>> Projects => _projects;
    public decimal Remaining => TimesheetEvaluator.Normalize(_core + _projects.Values.Sum());

    public decimal Project(Guid id) => _projects[id];

    public void ConsumeCore(decimal amount) => _core = TimesheetEvaluator.Normalize(Math.Max(0m, _core - amount));

    public void ConsumeProject(Guid id, decimal amount) => _projects[id] = TimesheetEvaluator.Normalize(Math.Max(0m, _projects[id] - amount));

    public static MonthlyTargets Remainders(EditableTimesheet sheet, decimal totalWorkload) => new(
        TimesheetEvaluator.Normalize(Math.Max(0m, CoreTarget(sheet, totalWorkload) - sheet.Days.Sum(day => day.CoreHours))),
        sheet.ContractParts.ToDictionary(
            project => project.Id,
            project => TimesheetEvaluator.Normalize(Math.Max(0m, ContractPartTarget(sheet, project) - sheet.Days.Sum(day => day.ContractPartHours.GetValueOrDefault(project.Id))))));

    public static MonthlyTargets NonAcademicCapacityRemainders(EditableTimesheet sheet, decimal availableHours)
    {
        Dictionary<Guid, decimal> projectHours = sheet.ContractParts.ToDictionary(
            project => project.Id,
            project => TimesheetEvaluator.Normalize(sheet.Days.Sum(day => day.ContractPartHours.GetValueOrDefault(project.Id))));
        Dictionary<Guid, decimal> contractPartTargets = sheet.ContractParts.ToDictionary(
            project => project.Id,
            project => Math.Max(NonAcademicContractPartTarget(sheet, project), projectHours[project.Id]));
        decimal coreTarget = TimesheetEvaluator.Normalize(Math.Max(0m, availableHours - contractPartTargets.Values.Sum()));

        return new MonthlyTargets(
            TimesheetEvaluator.Normalize(Math.Max(0m, coreTarget - sheet.Days.Sum(day => day.CoreHours))),
            contractPartTargets.ToDictionary(
                item => item.Key,
                item => TimesheetEvaluator.Normalize(Math.Max(0m, item.Value - projectHours[item.Key]))));
    }

    public static decimal NonAcademicContractPartTarget(EditableTimesheet sheet, ContractPartColumn project) =>
        ContractPartTarget(sheet, project);

    public static decimal ContractPartTarget(EditableTimesheet sheet, ContractPartColumn project)
    {
        int fundedDays = sheet.Days.Count(day => TimesheetEvaluator.IsWorkday(day.Date, day.IsHoliday) && project.IsActiveOn(day.Date));
        return TimesheetEvaluator.Normalize(fundedDays * 8m * project.Workload);
    }

    public static decimal CoreTarget(EditableTimesheet sheet, decimal totalWorkload)
    {
        int fundedDays = sheet.Days.Count(day => TimesheetEvaluator.IsWorkday(day.Date, day.IsHoliday));
        decimal total = TimesheetEvaluator.Normalize(fundedDays * 8m * totalWorkload);
        return TimesheetEvaluator.Normalize(total - sheet.ContractParts.Sum(project => ContractPartTarget(sheet, project)));
    }

    public static void AppendMismatch(List<string> errors, string label, decimal actual, decimal expected)
    {
        if (TimesheetEvaluator.HasUnequalHours(actual, expected))
        {
            errors.Add($"{label} {actual:F2}/{expected:F2}");
        }
    }
}

internal sealed class HumanHours
{
    public decimal RandomDayHours()
    {
        decimal raw = Random.Shared.NextDouble() < 0.7
            ? 7m + (decimal)Random.Shared.NextDouble() * 2m
            : 6m + (decimal)Random.Shared.NextDouble() * 6m;
        return Math.Min(12m, Math.Max(6m, Humanize(raw)));
    }

    public decimal RandomAmount(decimal max)
    {
        max = TimesheetEvaluator.Normalize(max);
        if (max <= 1m)
        {
            return max;
        }

        decimal raw = 1m + (decimal)Random.Shared.NextDouble() * (max - 1m);
        decimal amount = Math.Min(max, Humanize(raw));
        return max - amount is > 0m and < 1m ? max : amount;
    }

    private static decimal Humanize(decimal raw)
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
            return TimesheetEvaluator.Normalize(Math.Round(raw * 10m, MidpointRounding.AwayFromZero) / 10m);
        }

        return TimesheetEvaluator.Normalize(raw);
    }

    public static decimal RoundToHalfHour(decimal value) => TimesheetEvaluator.Normalize(Math.Round(value * 2m, MidpointRounding.AwayFromZero) / 2m);
    public static decimal RoundToQuarter(decimal value) => TimesheetEvaluator.Normalize(Math.Round(value * 4m, MidpointRounding.AwayFromZero) / 4m);
    public static decimal RoundUpToHalfHour(decimal value) => TimesheetEvaluator.Normalize(Math.Ceiling(value * 2m) / 2m);
}

internal sealed class DayTargetFiller(IReadOnlyList<ContractPartColumn> projects, decimal totalWorkload, bool tracksAttendance, MonthlyTargets targets)
{
    public void Fill(EditableTimesheetDay day)
    {
        if (TimesheetEvaluator.HasBusinessTripInterruption(day.Description))
        {
            return;
        }
        if (day.HasLockedContractPartHours())
        {
            return;
        }

        if (TimesheetEvaluator.HasProportionalInterruption(day.Description))
        {
            decimal previousCoreHours = day.CoreHours;
            Dictionary<Guid, decimal> previousContractPartHours = projects.ToDictionary(project => project.Id, project => day.ContractPartHours.GetValueOrDefault(project.Id));
            TimesheetEvaluator.ApplyInterruptionToDayState(day, projects, totalWorkload, tracksAttendance);
            targets.ConsumeCore(day.CoreHours - previousCoreHours);
            foreach (ContractPartColumn project in projects)
            {
                targets.ConsumeProject(project.Id, day.ContractPartHours.GetValueOrDefault(project.Id) - previousContractPartHours[project.Id]);
            }

            return;
        }

        decimal capacity = TimesheetEvaluator.DayCapacity(day.Date, day.ClockIn, day.ClockOut, day.BreakStart, day.BreakEnd, day.Description, totalWorkload, tracksAttendance, day.Schedules);
        decimal free = TimesheetEvaluator.Normalize(capacity - day.CoreHours - day.ContractPartHours.Values.Sum());
        if (free <= 0)
        {
            return;
        }

        decimal stagHours = TimesheetEvaluator.CalculateStagHours(day.Schedules);
        decimal stagMissing = Math.Max(0m, stagHours - day.CoreHours);
        bool coreCanReceiveRemainder = !day.CoreHoursFixed && targets.Core > 0m;
        if (!day.CoreHoursFixed && stagMissing > 0)
        {
            decimal core = PreferStagTopUpHours(day.CoreHours, stagHours, free, targets.Core);
            day.CoreHours += core;
            targets.ConsumeCore(core);
            free -= core;
        }

        List<(Guid ContractEmployeeId, decimal Remaining)> projectRemaining = [];
        foreach (ContractPartColumn project in projects)
        {
            bool fixedHours = day.ContractPartHoursFixed.GetValueOrDefault(project.Id);
            if (project.IsActiveOn(day.Date) && !project.Locked && !fixedHours && day.ContractPartHours.GetValueOrDefault(project.Id) <= day.ProjectFloor(project.Id) && targets.Project(project.Id) > 0)
            {
                projectRemaining.Add((project.Id, targets.Project(project.Id)));
            }
        }

        decimal coreRemaining = coreCanReceiveRemainder ? targets.Core : 0m;
        decimal totalRemaining = coreRemaining + projectRemaining.Sum(item => item.Remaining);
        decimal amount = Math.Min(free, totalRemaining);
        decimal left = amount;

        foreach ((Guid contractEmployeeId, decimal target) in projectRemaining)
        {
            decimal maxValue = Math.Min(target, left);
            decimal value = PreferGeneratedCellHours(TimesheetEvaluator.Normalize(amount * target / totalRemaining), maxValue);
            left -= value;
            day.SetContractPartHours(contractEmployeeId, value);
            targets.ConsumeProject(contractEmployeeId, value);
        }

        if (!day.CoreHoursFixed && left > 0m)
        {
            day.CoreHours += left;
            targets.ConsumeCore(left);
            left = 0m;
        }
    }

    private static decimal PreferStagTopUpHours(decimal currentCoreHours, decimal stagHours, decimal free, decimal coreTarget)
    {
        decimal required = TimesheetEvaluator.Normalize(stagHours - currentCoreHours);
        if (required <= 0m)
        {
            return 0m;
        }

        decimal roundedFinal = HumanHours.RoundUpToHalfHour(stagHours);
        decimal rounded = TimesheetEvaluator.Normalize(Math.Max(required, roundedFinal - currentCoreHours));
        return rounded <= free && rounded <= Math.Max(required, coreTarget) ? rounded : Math.Min(required, free);
    }

    private static decimal PreferGeneratedCellHours(decimal value, decimal max)
    {
        max = TimesheetEvaluator.Normalize(max);
        if (max < 0.5m)
        {
            return 0m;
        }

        decimal rounded = Math.Max(0.5m, HumanHours.RoundToHalfHour(value));
        if (rounded > max)
        {
            rounded = Math.Floor(max * 2m) / 2m;
        }

        return rounded;
    }
}
