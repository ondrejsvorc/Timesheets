using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Auth;

public sealed class UserSynchronizer(AppDbContext dbContext)
{
    private readonly record struct SynchronizedUser(string FullName, string PersonalNumber, string? TitleBefore, string? TitleAfter, Guid EmployeeTypeId);

    public async Task SyncFromPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        SynchronizedUser synchronizedUser = new(
            principal.GetFullName(),
            principal.GetPersonalNumber(),
            principal.GetTitleBefore(),
            principal.GetTitleAfter(),
            principal.GetEmployeeTypeId());
        await SyncUserAsync(synchronizedUser, cancellationToken);
    }

    private async Task SyncUserAsync(SynchronizedUser user, CancellationToken cancellationToken)
    {
        Employee? existing = await dbContext.Employees.FirstOrDefaultAsync(e => e.PersonalNumber == user.PersonalNumber, cancellationToken);

        if (existing is null)
        {
            await CreateEmployeeAsync(user, cancellationToken);
        }
        else
        {
            await UpdateEmployeeAsync(existing, user, cancellationToken);
        }
    }

    private async Task CreateEmployeeAsync(SynchronizedUser user, CancellationToken cancellationToken)
    {
        Employee employee = new()
        {
            Id = Guid.NewGuid(),
            FullName = user.FullName,
            PersonalNumber = user.PersonalNumber,
            TitleBefore = user.TitleBefore,
            TitleAfter = user.TitleAfter,
            IsGlobalManager = false,
            EmployeeTypeId = user.EmployeeTypeId,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateEmployeeAsync(Employee existing, SynchronizedUser user, CancellationToken cancellationToken)
    {
        bool hasChanges = false;

        if (existing.FullName != user.FullName)
        {
            existing.FullName = user.FullName;
            hasChanges = true;
        }

        if (existing.PersonalNumber != user.PersonalNumber)
        {
            existing.PersonalNumber = user.PersonalNumber;
            hasChanges = true;
        }

        if (existing.TitleBefore != user.TitleBefore)
        {
            existing.TitleBefore = user.TitleBefore;
            hasChanges = true;
        }

        if (existing.TitleAfter != user.TitleAfter)
        {
            existing.TitleAfter = user.TitleAfter;
            hasChanges = true;
        }

        if (existing.EmployeeTypeId != user.EmployeeTypeId)
        {
            existing.EmployeeTypeId = user.EmployeeTypeId;
            hasChanges = true;
        }

        if (!hasChanges)
        {
            return;
        }

        existing.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
