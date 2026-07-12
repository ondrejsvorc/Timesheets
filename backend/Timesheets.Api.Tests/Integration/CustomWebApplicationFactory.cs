using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Timesheets.Api.Domain;

namespace Timesheets.Api.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;

    public CustomWebApplicationFactory()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("public.ecr.aws/docker/library/postgres:17-alpine")
            .WithDatabase("timesheets_test_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__Database", _dbContainer.GetConnectionString());
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });

            // Register TestAuthHandler
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TestScheme";
                options.DefaultChallengeScheme = "TestScheme";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });
        });

        builder.UseEnvironment("Development");
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        using var scope = Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        var db = scopedServices.GetRequiredService<AppDbContext>();
        var logger = scopedServices.GetRequiredService<ILogger<CustomWebApplicationFactory>>();

        try
        {
            await db.Database.MigrateAsync();
            await TestEmployeeFactory.CreateAsync(db, TestAuthHandler.PersonalNumber, "Test", "User", isGlobalManager: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred applying the database migrations.");
            throw;
        }
    }

    new public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }
}
