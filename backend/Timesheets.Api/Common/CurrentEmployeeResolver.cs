using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Domain;
using Timesheets.Api.Domain.Models;

namespace Timesheets.Api.Common;

public static class CurrentEmployeeResolver
{
    public static async Task<Employee?> TryGetAsync(ClaimsPrincipal principal, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!principal.IsAuthenticated())
        {
            return null;
        }

        string personalNumber = principal.GetPersonalNumber();

        return await dbContext.Employees
            .AsNoTracking()
            .Include(e => e.EmployeeType)
            .FirstOrDefaultAsync(e => e.PersonalNumber == personalNumber, cancellationToken);
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
