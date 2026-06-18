using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Contracts;

internal static class ContractEmployeeValidation
{
    public static string? ValidateProjectRange(DateTime projectStartDate, DateTime? projectEndDate, DateTime startDate, DateTime? endDate)
    {
        DateTime projectStart = ToUtcDate(projectStartDate);
        DateTime start = ToUtcDate(startDate);
        DateTime? projectEnd = projectEndDate.HasValue ? ToUtcDate(projectEndDate.Value) : null;
        DateTime? end = endDate.HasValue ? ToUtcDate(endDate.Value) : null;

        if (start < projectStart)
        {
            return "Začátek úvazku nesmí být před začátkem projektu.";
        }

        if (projectEnd.HasValue && start > projectEnd.Value)
        {
            return "Začátek úvazku musí být nejpozději v den ukončení projektu.";
        }

        if (projectEnd.HasValue && end.HasValue && end.Value > projectEnd.Value)
        {
            return "Konec úvazku musí být nejpozději v den ukončení projektu.";
        }

        return null;
    }

    public static async Task<bool> HasOverlappingSamePositionAsync(Guid contractId, Guid employeeId, string position, DateTime startDate, DateTime? endDate, Guid? excludeContractEmployeeId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        return await dbContext.ContractEmployees
            .AsNoTracking()
            .AnyAsync(
                ce =>
                    ce.ContractId == contractId
                    && ce.EmployeeId == employeeId
                    && ce.Position == position
                    && (excludeContractEmployeeId == null || ce.Id != excludeContractEmployeeId.Value)
                    && (ce.EndDate == null || startDate <= ce.EndDate)
                    && (endDate == null || ce.StartDate <= endDate),
                cancellationToken);
    }

    public static async Task<string?> ValidateMonthlyWorkloadAsync(Guid employeeId, decimal additionalWorkload, DateTime startDate, DateTime? endDate, Guid? excludeContractEmployeeId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        DateTime start = ToUtcDate(startDate);
        DateTime end = endDate.HasValue ? ToUtcDate(endDate.Value) : start;

        DateTime cursor = new(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime last = new(end.Year, end.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        while (cursor <= last)
        {
            int year = cursor.Year;
            int month = cursor.Month;

            decimal? baseWorkload = await GetBaseWorkloadAsync(employeeId, year, month, dbContext, cancellationToken);
            decimal baseForValidation = baseWorkload is > 0m ? baseWorkload.Value : 1m;

            decimal currentProjectWorkload = await dbContext.ProjectTimesheets
                .AsNoTracking()
                .Where(t => t.EmployeeId == employeeId && t.Year == year && t.Month == month)
                .Where(t => excludeContractEmployeeId == null || t.ContractEmployeeId != excludeContractEmployeeId.Value)
                .SumAsync(t => (decimal?)t.Workload, cancellationToken) ?? 0m;

            if (currentProjectWorkload + additionalWorkload > baseForValidation)
            {
                return $"Nelze přiřadit pozici. Překročil by se celkový úvazek pro {month:00}/{year}.";
            }

            cursor = cursor.AddMonths(1);
        }

        return null;
    }

    private static async Task<decimal?> GetBaseWorkloadAsync(Guid employeeId, int year, int month, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        DateTime periodStart = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime periodEnd = periodStart.AddMonths(1).AddDays(-1);

        decimal? monthly = await dbContext.EmployeeWorkloads
            .AsNoTracking()
            .Where(w => w.EmployeeId == employeeId && w.Year == year && w.Month == month)
            .Select(w => (decimal?)w.Workload)
            .FirstOrDefaultAsync(cancellationToken);
        if (monthly.HasValue)
        {
            return monthly.Value;
        }

        return await dbContext.CoreEmployments
            .AsNoTracking()
            .Where(e => e.EmployeeId == employeeId)
            .Where(e => e.StartDate <= periodEnd && (e.EndDate == null || e.EndDate >= periodStart))
            .OrderByDescending(e => e.StartDate)
            .Select(e => (decimal?)e.Workload)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static DateTime ToUtcDate(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value.Date : DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
}
