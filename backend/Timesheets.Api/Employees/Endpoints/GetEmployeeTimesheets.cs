using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Timesheets.Api.Administration;
using Timesheets.Api.Auth;
using Timesheets.Api.Data;

namespace Timesheets.Api.Employees.Endpoints;

public sealed class GetEmployeeTimesheets : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}/timesheets", Handle)
           .WithSummary("Get Employee Timesheets");

    public sealed record Request([FromQuery] int? Year, [FromQuery] string? Months);
    public sealed record MonthItem(int Year, int Month, bool HasAttendanceImport, string? Status);
    public sealed record Response(Guid EmployeeId, IEnumerable<MonthItem> Months, IEnumerable<int> AvailableYears, IEnumerable<int> AvailableMonths);

    private static async Task<Results<Ok<Response>, NotFound, ForbidHttpResult>> Handle(
        Guid id,
        [AsParameters] Request request,
        HttpContext httpContext,
        AppDbContext dbContext,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        (_, UserPermissionsScope scope) = await PermissionsScopeResolver.ResolveRequiredAsync(
            httpContext, dbContext, administrationOptions, cancellationToken);

        if (!await ApiPermissions.CanAccessEmployeeAsync(scope, id, dbContext, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        bool employeeExists = await dbContext.Employees
            .AsNoTracking()
            .AnyAsync(e => e.Id == id, cancellationToken);

        if (!employeeExists)
        {
            return TypedResults.NotFound();
        }

        // Build month list from either assigned projects (ProjectTimesheets) or imported attendance (AttendanceTimesheets).
        var monthKeysQuery =
            dbContext.ProjectTimesheets.AsNoTracking()
                .Where(t => t.EmployeeId == id)
                .Select(t => new { t.Year, t.Month })
                .Union(
                    dbContext.AttendanceTimesheets.AsNoTracking()
                        .Where(t => t.EmployeeId == id)
                        .Select(t => new { t.Year, t.Month })
                );

        if (request.Year.HasValue)
        {
            monthKeysQuery = monthKeysQuery.Where(m => m.Year == request.Year.Value);
        }

        List<int>? requestedMonths = null;
        if (!string.IsNullOrWhiteSpace(request.Months))
        {
            requestedMonths = request.Months
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out int m) ? m : 0)
                .Where(m => m >= 1 && m <= 12)
                .Distinct()
                .ToList();
            if (requestedMonths.Count > 0)
            {
                monthKeysQuery = monthKeysQuery.Where(m => requestedMonths.Contains(m.Month));
            }
        }

        List<(int Year, int Month)> monthKeys = await monthKeysQuery
            .Distinct()
            .OrderBy(m => m.Year)
            .ThenBy(m => m.Month)
            .Select(m => new ValueTuple<int, int>(m.Year, m.Month))
            .ToListAsync(cancellationToken);

        HashSet<(int Year, int Month)> importedAttendance = await dbContext.EmployeeWorkloads
            .AsNoTracking()
            .Where(w => w.EmployeeId == id)
            .Select(w => new ValueTuple<int, int>(w.Year, w.Month))
            .ToHashSetAsync(cancellationToken);

        Dictionary<(int Year, int Month), string> statusByMonth = await dbContext.AttendanceTimesheets
            .AsNoTracking()
            .Where(t => t.EmployeeId == id)
            .Select(t => new { t.Year, t.Month, Status = t.TimesheetStatus.Name })
            .ToDictionaryAsync(t => (t.Year, t.Month), t => t.Status, cancellationToken);

        List<MonthItem> months = monthKeys
            .Select(k => new MonthItem(
                k.Year,
                k.Month,
                importedAttendance.Contains(k),
                statusByMonth.GetValueOrDefault(k)))
            .ToList();

        List<int> availableYears = months
            .Select(m => m.Year)
            .Distinct()
            .OrderBy(y => y)
            .ToList();

        List<int> availableMonths = months
            .Where(m => !request.Year.HasValue || m.Year == request.Year.Value)
            .Select(m => m.Month)
            .Distinct()
            .OrderBy(m => m)
            .ToList();

        return TypedResults.Ok(new Response(id, months, availableYears, availableMonths));
    }
}
