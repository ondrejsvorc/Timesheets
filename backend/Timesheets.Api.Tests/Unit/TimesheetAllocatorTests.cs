using Timesheets.Api.Domain.Models;
using Timesheets.Api.Features.Employees;
using Timesheets.Api.Features.Timesheets;
using Timesheets.Api.Features.Timesheets.Allocation;
using DataTimesheet = Timesheets.Api.Domain.Models.Timesheet;

namespace Timesheets.Api.Tests.Unit;

public sealed class TimesheetAllocatorTests
{
    private static readonly DateTime Workday = new(2036, 1, 2, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void AllocateDay_PartiallyReducesMutableProjectBeforeCore()
    {
        Guid lockedProject = Guid.CreateVersion7();
        Guid mutableProject = Guid.CreateVersion7();
        EditableTimesheet sheet = Sheet(
            coreHours: 4m,
            (lockedProject, 2.5m, true),
            (mutableProject, 2.5m, false));

        new TimesheetAllocator().AllocateDay(LoadedNonAcademicTimesheet(), sheet, Workday.Day);

        EditableTimesheetDay day = sheet.Days.Single();
        Assert.Equal(4m, day.CoreHours);
        Assert.Equal(2.5m, day.ContractPartHours[lockedProject]);
        Assert.Equal(1.5m, day.ContractPartHours[mutableProject]);
        Assert.Equal(8m, TotalHours(day));
    }

    [Fact]
    public void AllocateDay_PreservesMutableProjectWhenCoreCanFillShortfall()
    {
        Guid lockedProject = Guid.CreateVersion7();
        Guid mutableProject = Guid.CreateVersion7();
        EditableTimesheet sheet = Sheet(
            coreHours: 3m,
            (lockedProject, 2m, true),
            (mutableProject, 2m, false));

        new TimesheetAllocator().AllocateDay(LoadedNonAcademicTimesheet(), sheet, Workday.Day);

        EditableTimesheetDay day = sheet.Days.Single();
        Assert.Equal(4m, day.CoreHours);
        Assert.Equal(2m, day.ContractPartHours[lockedProject]);
        Assert.Equal(2m, day.ContractPartHours[mutableProject]);
        Assert.Equal(8m, TotalHours(day));
    }

    private static EditableTimesheet Sheet(decimal coreHours, params (Guid Id, decimal Hours, bool Fixed)[] projects)
    {
        Dictionary<Guid, decimal> hours = projects.ToDictionary(project => project.Id, project => project.Hours);
        Dictionary<Guid, bool> fixedHours = projects.ToDictionary(project => project.Id, project => project.Fixed);
        Dictionary<Guid, decimal> floors = projects.ToDictionary(project => project.Id, project => project.Fixed ? project.Hours : 0m);
        ContractPartDateRange range = new(Workday, Workday);

        return new EditableTimesheet(
            Days:
            [
                new EditableTimesheetDay
                {
                    Date = Workday,
                    ClockIn = new TimeSpan(8, 0, 0),
                    ClockOut = new TimeSpan(16, 0, 0),
                    BreakStart = null,
                    BreakEnd = null,
                    Description = null,
                    Schedules = [],
                    IsHoliday = false,
                    CoreHours = coreHours,
                    CoreHoursFixed = false,
                    ContractPartHours = hours,
                    ContractPartHoursFixed = fixedHours,
                    ContractPartHoursFloor = floors
                }
            ],
            ContractParts: projects.Select(project => new ContractPartColumn(project.Id, 0.25m, false, range)).ToArray());
    }

    private static LoadedTimesheet LoadedNonAcademicTimesheet()
    {
        Employee employee = new()
        {
            Id = Guid.CreateVersion7(),
            EmployeeTypeId = EmployeeTypes.NonAcademicId,
            PersonalNumber = "1",
            FirstName = "Test",
            Surname = "Employee"
        };
        DataTimesheet timesheet = new()
        {
            Id = Guid.CreateVersion7(),
            EmployeeId = employee.Id,
            Employee = employee,
            Year = Workday.Year,
            Month = Workday.Month
        };
        Attendance attendance = new()
        {
            Id = timesheet.Id,
            TimesheetId = timesheet.Id,
            EmployeeTypeId = EmployeeTypes.NonAcademicId
        };

        return new LoadedTimesheet(timesheet, attendance, ContractParts: [], ContractPartRanges: new Dictionary<Guid, ContractPartDateRange>(), TotalWorkload: 1m, CoreWorkload: 0.5m);
    }

    private static decimal TotalHours(EditableTimesheetDay day) =>
        TimesheetEvaluator.Normalize(day.CoreHours + day.ContractPartHours.Values.Sum());
}
