using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Common;

public static class EmployeeRoleFormatter
{
    public static string FormatApiRole(Employee employee) => employee.IsGlobalManager ? "Manager" : "Employee";
}
