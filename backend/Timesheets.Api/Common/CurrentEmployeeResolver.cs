using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Common;

public static class CurrentEmployeeResolver
{
    public static async Task<Employee?> TryGetAsync(ClaimsPrincipal principal, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!principal.IsAuthenticated())
        {
            return null;
        }

        string email = principal.GetEmail();
        return await dbContext.Employees
            .AsNoTracking()
            .Include(e => e.EmployeeType)
            .FirstOrDefaultAsync(e => e.Email == email, cancellationToken);
    }

    public static async Task<Employee> GetRequiredAsync(ClaimsPrincipal principal, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Employee? employee = await TryGetAsync(principal, dbContext, cancellationToken);
        if (employee is null)
        {
            throw new InvalidOperationException("Employee not found for current user.");
        }

        return employee;
    }
}
