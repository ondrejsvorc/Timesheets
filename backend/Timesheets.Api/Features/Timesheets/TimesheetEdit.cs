using FluentValidation;

namespace Timesheets.Api.Features.Timesheets;

public sealed record DayEdit(DateTime Date, TimeSpan? ClockIn, TimeSpan? ClockOut, TimeSpan? BreakStart, TimeSpan? BreakEnd, decimal CoreHours, string? Description, IReadOnlyList<TimeRange>? Schedules, bool CoreHoursFixed = false);

public sealed record ContractPartDayEdit(DateTime Date, decimal Hours, bool HoursLocked = false);

public sealed record ContractPartEdit(Guid ContractEmployeeId, IReadOnlyList<ContractPartDayEdit> Days);

public sealed record TimesheetEdit(IReadOnlyList<DayEdit> Days, IReadOnlyList<ContractPartEdit>? ContractParts);

public sealed class TimesheetEditValidator : AbstractValidator<TimesheetEdit>
{
    public TimesheetEditValidator()
    {
        RuleFor(request => request.Days).NotEmpty().Must(HaveUniqueDates);
        RuleFor(request => request.ContractParts).Must(HaveUniqueContractParts);
        RuleForEach(request => request.Days).ChildRules(day =>
        {
            day.RuleFor(value => value.CoreHours).InclusiveBetween(0m, 12m);
            day.RuleFor(value => value.ClockIn).Must(IsTimeOfDay);
            day.RuleFor(value => value.ClockOut).Must(IsTimeOfDay);
            day.RuleFor(value => value.BreakStart).Must(IsTimeOfDay);
            day.RuleFor(value => value.BreakEnd).Must(IsTimeOfDay);
        });
        RuleForEach(request => request.ContractParts).ChildRules(project =>
        {
            project.RuleFor(value => value.Days).Must(HaveUniqueDates);
            project.RuleFor(value => value.Days).Must(HaveAtMostOneNonHalfHour);
            project.RuleForEach(value => value.Days).ChildRules(day =>
            {
                day.RuleFor(value => value.Hours).InclusiveBetween(0m, 12m);
            });
        });
    }

    private static bool IsHalfHourIncrement(decimal hours) => hours * 2m % 1m == 0m;
    private static bool HaveAtMostOneNonHalfHour(IEnumerable<ContractPartDayEdit> days) => days.Count(day => !IsHalfHourIncrement(day.Hours)) <= 1;

    private static bool IsTimeOfDay(TimeSpan? value) => value is null || value >= TimeSpan.Zero && value < TimeSpan.FromDays(1);
    private static bool HaveUniqueDates(IEnumerable<DayEdit> days) => days.Select(day => DateOnly.FromDateTime(day.Date)).Distinct().Count() == days.Count();
    private static bool HaveUniqueDates(IEnumerable<ContractPartDayEdit> days) => days.Select(day => DateOnly.FromDateTime(day.Date)).Distinct().Count() == days.Count();
    private static bool HaveUniqueContractParts(IEnumerable<ContractPartEdit>? projects) => projects is null || projects.Select(project => project.ContractEmployeeId).Distinct().Count() == projects.Count();
}
