using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Tests.Integration;

internal static class TestEmployeeFactory
{
    public static readonly Guid DefaultEmployeeTypeId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static async Task<Employee> CreateAsync(
        AppDbContext dbContext,
        string personalNumber,
        string fullName,
        Guid? employeeTypeId = null,
        bool isGlobalManager = false,
        CancellationToken cancellationToken = default)
    {
        Employee employee = new()
        {
            Id = Guid.NewGuid(),
            EmployeeTypeId = employeeTypeId ?? DefaultEmployeeTypeId,
            PersonalNumber = personalNumber,
            FullName = fullName,
            IsGlobalManager = isGlobalManager,
            CreatedAt = DateTime.UtcNow,
        };

        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync(cancellationToken);
        return employee;
    }

    public static async Task<Employee> CreateAsync(
        IServiceProvider services,
        string personalNumber,
        string fullName,
        Guid? employeeTypeId = null,
        bool isGlobalManager = false,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await CreateAsync(dbContext, personalNumber, fullName, employeeTypeId, isGlobalManager, cancellationToken);
    }
}
