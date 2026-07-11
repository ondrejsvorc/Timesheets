using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Features.Timesheets;

internal sealed record CombinedTimesheetScope(Guid TimesheetId, IReadOnlyDictionary<Guid, string> ContractPartLabels);

internal static class CombinedTimesheetScopeLoader
{
    public static async Task<CombinedTimesheetScope?> LoadAsync(Guid employeeId, int year, int month, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Guid? timesheetId = await dbContext.Timesheets
            .AsNoTracking()
            .Where(timesheet => timesheet.EmployeeId == employeeId && timesheet.Year == year && timesheet.Month == month)
            .Select(timesheet => (Guid?)timesheet.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (timesheetId is null)
        {
            return null;
        }

        List<(Guid Id, string ContractRegistrationNumber)> projectRows = await dbContext.ContractParts
            .AsNoTracking()
            .Where(part => part.TimesheetId == timesheetId.Value)
            .Join(dbContext.ContractEmployees.AsNoTracking(), timesheet => timesheet.ContractEmployeeId, contractEmployee => contractEmployee.Id, (timesheet, contractEmployee) => new { timesheet, contractEmployee })
            .Join(dbContext.Contracts.AsNoTracking(), x => x.contractEmployee.ContractId, contract => contract.Id, (x, contract) => new { x.timesheet.Id, contract.RegistrationNumber })
            .OrderBy(x => x.RegistrationNumber)
            .Select(x => new ValueTuple<Guid, string>(x.Id, x.RegistrationNumber))
            .ToListAsync(cancellationToken);

        Dictionary<Guid, string> labels = projectRows.ToDictionary(row => row.Id, row => row.ContractRegistrationNumber);

        return new CombinedTimesheetScope(timesheetId.Value, labels);
    }

    public static string ResolveTimesheetLabel(this CombinedTimesheetScope scope, Guid? timesheetId, Guid? contractPartId)
    {
        if (timesheetId is not null)
        {
            return "Pracovní výkaz";
        }

        if (contractPartId is not null && scope.ContractPartLabels.TryGetValue(contractPartId.Value, out string? label))
        {
            return label;
        }

        return "Výkaz";
    }
}
