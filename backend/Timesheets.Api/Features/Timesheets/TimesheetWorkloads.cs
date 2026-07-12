using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;

namespace Timesheets.Api.Features.Timesheets;

internal static class TimesheetWorkloads
{
    public static async Task<decimal> GetAsync(Guid employeeId, int year, int month, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        decimal? monthly = await dbContext.EmployeeWorkloads
            .AsNoTracking()
            .Where(workload => workload.EmployeeId == employeeId && workload.Year == year && workload.Month == month)
            .Select(workload => (decimal?)workload.Workload)
            .FirstOrDefaultAsync(cancellationToken);

        if (monthly.HasValue)
        {
            return monthly.Value;
        }

        DateTime periodStart = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime periodEnd = periodStart.AddMonths(1).AddDays(-1);

        return await dbContext.CoreEmployments
            .AsNoTracking()
            .Where(employment => employment.EmployeeId == employeeId)
            .Where(employment => employment.StartDate <= periodEnd && (employment.EndDate == null || employment.EndDate >= periodStart))
            .OrderByDescending(employment => employment.StartDate)
            .Select(employment => (decimal?)employment.Workload)
            .FirstOrDefaultAsync(cancellationToken) ?? 0m;
    }
}
