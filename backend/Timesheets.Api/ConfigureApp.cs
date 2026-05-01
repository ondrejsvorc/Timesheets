using System.Security.Claims;
using Microsoft.AspNetCore.HttpOverrides;
using Timesheets.Api.Auth;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;

namespace Timesheets.Api;

public static class ConfigureApp
{
    public static void Configure(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Timesheets API"));
        }

        app.UseForwardedHeaders();

        bool authEnabled = AuthenticationConfig.IsEnabled(app.Configuration);
        if (authEnabled)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }
        else if (app.Environment.IsDevelopment())
        {
            // Dev-only "no-auth" mode: make HttpContext.User look authenticated so endpoints relying on claims work.
            // Values can be overridden via config under Authentication:DevUser:*.
            app.Use(async (context, next) =>
            {
                context.User = AuthenticationConfig.CreateDevPrincipal(context.RequestServices.GetRequiredService<IConfiguration>());
                await next();
            });
        }
        app.ApplyMigrations();

        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            DatabaseSeeder.SeedTestDataAsync(context).GetAwaiter().GetResult();
        }

        if (!authEnabled && app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            var synchronizer = scope.ServiceProvider.GetRequiredService<UserSynchronizer>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            synchronizer.SyncFromPrincipalAsync(AuthenticationConfig.CreateDevPrincipal(configuration), CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        app.UseCors();
        // In local docker dev we often run HTTP only; forcing HTTPS causes redirect loops / fetch failures.
        // Enable explicitly via config if needed.
        if (app.Configuration.GetValue("HttpsRedirection:Enabled", false))
        {
            app.UseHttpsRedirection();
        }
        app.UseResponseCompression();
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/api/health");
        app.MapEndpoints();
    }
}
