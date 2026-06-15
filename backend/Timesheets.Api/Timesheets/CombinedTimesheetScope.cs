using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Timesheets;

internal sealed record CombinedTimesheetScope(Guid AttendanceTimesheetId, IReadOnlyDictionary<Guid, string> ProjectTimesheetLabels);

internal static class CombinedTimesheetScopeLoader
{
    public static async Task<CombinedTimesheetScope?> LoadAsync(Guid employeeId, int year, int month, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Guid? attendanceTimesheetId = await dbContext.AttendanceTimesheets
            .AsNoTracking()
            .Where(t => t.EmployeeId == employeeId && t.Year == year && t.Month == month)
            .Select(t => (Guid?)t.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (attendanceTimesheetId is null)
        {
            return null;
        }

        List<(Guid Id, string ContractRegistrationNumber)> projectRows = await dbContext.ProjectTimesheets
            .AsNoTracking()
            .Where(timesheet => timesheet.EmployeeId == employeeId && timesheet.Year == year && timesheet.Month == month)
            .Join(dbContext.ContractEmployees.AsNoTracking(), timesheet => timesheet.ContractEmployeeId, contractEmployee => contractEmployee.Id, (timesheet, contractEmployee) => new { timesheet, contractEmployee })
            .Join(dbContext.Contracts.AsNoTracking(), x => x.contractEmployee.ContractId, contract => contract.Id, (x, contract) => new { x.timesheet.Id, contract.RegistrationNumber })
            .OrderBy(x => x.RegistrationNumber)
            .Select(x => new ValueTuple<Guid, string>(x.Id, x.RegistrationNumber))
            .ToListAsync(cancellationToken);

        Dictionary<Guid, string> labels = projectRows.ToDictionary(row => row.Id, row => row.ContractRegistrationNumber);

        return new CombinedTimesheetScope(attendanceTimesheetId.Value, labels);
    }

    public static string ResolveTimesheetLabel(this CombinedTimesheetScope scope, Guid? attendanceTimesheetId, Guid? projectTimesheetId)
    {
        if (attendanceTimesheetId is not null)
        {
            return "Pracovní výkaz";
        }

        if (projectTimesheetId is not null && scope.ProjectTimesheetLabels.TryGetValue(projectTimesheetId.Value, out string? label))
        {
            return label;
        }

        return "Výkaz";
    }
}
