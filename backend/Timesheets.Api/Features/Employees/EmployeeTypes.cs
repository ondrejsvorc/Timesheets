namespace Timesheets.Api.Features.Employees;

public static class EmployeeTypes
{
    public static readonly Guid AcademicId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid NonAcademicId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public static bool IsAcademic(Guid? employeeTypeId) => employeeTypeId == AcademicId;

    public static bool TracksAttendance(Guid? employeeTypeId) => !IsAcademic(employeeTypeId);
}
