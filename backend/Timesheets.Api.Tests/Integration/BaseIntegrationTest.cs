using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Tests.Integration;

[Collection("Integration")]
public abstract class BaseIntegrationTest
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    protected BaseIntegrationTest(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    /// <summary>
    /// Creates a new dependency injection scope. 
    /// Useful for getting a clean instance of AppDbContext for assertions.
    /// </summary>
    protected IServiceScope CreateScope()
    {
        return Factory.Services.CreateScope();
    }

    protected async Task<Guid> SeedEmployeeAsync(string personalNumber, string firstName, string surname, Guid? employeeTypeId = null, CancellationToken cancellationToken = default)
    {
        Employee employee = await TestEmployeeFactory.CreateAsync(Factory.Services, personalNumber, firstName, surname, employeeTypeId, cancellationToken: cancellationToken);
        return employee.Id;
    }
}
