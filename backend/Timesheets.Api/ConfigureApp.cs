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

        //app.UseAuthentication();
        //app.UseAuthorization();
        app.ApplyMigrations();

        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            DatabaseSeeder.SeedTestDataAsync(context).GetAwaiter().GetResult();
        }

        app.UseCors();
        app.UseHttpsRedirection();
        app.UseResponseCompression();
        app.MapHealthChecks("/health");
        app.MapEndpoints();
    }
}
