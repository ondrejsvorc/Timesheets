using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Domain;
using Timesheets.Api.Domain.Models;

namespace Timesheets.Api.Tests.Integration;

internal static class TestEmployeeFactory
{
    public static readonly Guid DefaultEmployeeTypeId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static async Task<Employee> CreateAsync(
        AppDbContext dbContext,
        string personalNumber,
        string firstName,
        string surname,
        Guid? employeeTypeId = null,
        bool isGlobalManager = false,
        CancellationToken cancellationToken = default)
    {
        Employee employee = new()
        {
            Id = Guid.CreateVersion7(),
            EmployeeTypeId = employeeTypeId ?? DefaultEmployeeTypeId,
            PersonalNumber = personalNumber,
            FirstName = firstName,
            Surname = surname,
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
        string firstName,
        string surname,
        Guid? employeeTypeId = null,
        bool isGlobalManager = false,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await CreateAsync(dbContext, personalNumber, firstName, surname, employeeTypeId, isGlobalManager, cancellationToken);
    }
}
