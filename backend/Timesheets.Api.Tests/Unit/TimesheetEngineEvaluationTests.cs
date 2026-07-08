using Timesheets.Api.Features.Employees;
using Timesheets.Api.Features.Timesheets;

using DataAttendanceDay = Timesheets.Api.Data.Models.AttendanceDay;
using DataAttendanceTimesheet = Timesheets.Api.Data.Models.AttendanceTimesheet;
using DataEmployee = Timesheets.Api.Data.Models.Employee;

namespace Timesheets.Api.Tests.Unit;

public sealed class TimesheetEngineEvaluationTests
{
    [Fact]
    public void Evaluate_uses_holidays_for_monthly_obligation_totals()
    {
        TimesheetEvaluation evaluation = EvaluateJanuary2026(date => TimesheetLogic.IsWorkday(date, isHoliday: date.Day == 1) ? 8m : 0m);

        Assert.False(evaluation.HasErrors);
        Assert.Equal(168m, evaluation.Totals.AllocatedHours);
        Assert.Equal(168m, evaluation.Totals.HoursObligation);
        Assert.DoesNotContain(evaluation.Issues, issue => issue.Code == "ERR-COM-02");
    }

    [Fact]
    public void Evaluate_allows_overtime_against_holiday_adjusted_monthly_obligation()
    {
        TimesheetEvaluation evaluation = EvaluateJanuary2026(date => TimesheetLogic.IsWeekday(date) ? 8m : 0m);

        Assert.Equal(176m, evaluation.Totals.AllocatedHours);
        Assert.Equal(168m, evaluation.Totals.HoursObligation);
        Assert.DoesNotContain(evaluation.Issues, issue => issue.Code == "ERR-COM-02");
        Assert.DoesNotContain(evaluation.Issues, issue => issue.Code == "ERR-COM-03");
    }

    private static TimesheetEvaluation EvaluateJanuary2026(Func<DateTime, decimal> coreHours)
    {
        DataEmployee employee = new()
        {
            Id = Guid.CreateVersion7(),
            EmployeeTypeId = EmployeeTypes.AcademicId,
            PersonalNumber = "1",
            FullName = "Test Employee"
        };

        DataAttendanceTimesheet timesheet = new()
        {
            Id = Guid.CreateVersion7(),
            EmployeeId = employee.Id,
            EmployeeTypeId = EmployeeTypes.AcademicId,
            Employee = employee,
            Year = 2026,
            Month = 1,
            Days = Enumerable.Range(1, 31)
                .Select(day =>
                {
                    DateTime date = new(2026, 1, day, 0, 0, 0, DateTimeKind.Utc);
                    return new DataAttendanceDay
                    {
                        Id = Guid.CreateVersion7(),
                        Date = date,
                        IsHoliday = day == 1,
                        Workload = 1m,
                        CoreHours = coreHours(date)
                    };
                })
                .ToList()
        };

        LoadedTimesheet loaded = new(
            Timesheet: timesheet,
            EmployeeTypeId: EmployeeTypes.AcademicId,
            Projects: [],
            ProjectRanges: new Dictionary<Guid, ProjectDateRange>(),
            TotalWorkload: 1m,
            CoreWorkload: 1m);

        return TimesheetEngine.Evaluate(loaded, TimesheetEngine.CurrentEditRequest(loaded));
    }
}
