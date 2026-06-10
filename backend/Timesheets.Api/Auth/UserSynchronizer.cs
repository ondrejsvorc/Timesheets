using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Auth;

public sealed class UserSynchronizer(AppDbContext dbContext)
{
    private readonly record struct SynchronizedUser(string Email, string FullName, string PersonalNumber, string? TitleBefore, string? TitleAfter);

    public async Task SyncFromPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        string email = principal.GetEmail();
        string fullName = principal.GetFullName();
        string personalNumber = principal.GetPersonalNumber();
        string? titleBefore = principal.GetTitleBefore();
        string? titleAfter = principal.GetTitleAfter();
        SynchronizedUser user = new(email, fullName, personalNumber, titleBefore, titleAfter);
        await SyncAsync(user, cancellationToken);
    }

    private async Task SyncAsync(SynchronizedUser user, CancellationToken cancellationToken)
    {
        Employee? existing = await dbContext.Employees
            .FirstOrDefaultAsync(e => e.PersonalNumber == user.PersonalNumber, cancellationToken);

        if (existing is null)
        {
            existing = await dbContext.Employees
                .FirstOrDefaultAsync(e => e.Email == user.Email, cancellationToken);
        }

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
            Email = user.Email,
            PersonalNumber = user.PersonalNumber,
            TitleBefore = user.TitleBefore,
            TitleAfter = user.TitleAfter,
            IsGlobalManager = false,
            EmployeeTypeId = null,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateEmployeeAsync(Employee existing, SynchronizedUser user, CancellationToken cancellationToken)
    {
        bool changed = false;
        if (existing.FullName != user.FullName)
        {
            existing.FullName = user.FullName;
            changed = true;
        }
        if (existing.PersonalNumber != user.PersonalNumber)
        {
            existing.PersonalNumber = user.PersonalNumber;
            changed = true;
        }
        if (existing.TitleBefore != user.TitleBefore)
        {
            existing.TitleBefore = user.TitleBefore;
            changed = true;
        }
        if (existing.TitleAfter != user.TitleAfter)
        {
            existing.TitleAfter = user.TitleAfter;
            changed = true;
        }
        if (existing.Email != user.Email)
        {
            existing.Email = user.Email;
            changed = true;
        }
        if (changed)
        {
            existing.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
