using FluentValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;
using Timesheets.Api.Notifications;
using Timesheets.Api.Timesheets;
using AttendanceTimesheet = Timesheets.Api.Timesheets.AttendanceTimesheet;

namespace Timesheets.Api;

public static class ConfigureServices
{
    public static void AddServices(this WebApplicationBuilder builder)
    {
        builder.AddOpenApi();
        builder.AddAuthenticationLayer();
        builder.AddDatabase();
        builder.AddAppServices();
    }

    private static void AddOpenApi(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi(options =>
        {
            options.CreateSchemaReferenceId = info =>
            {
                Type type = info.Type;
                if (type.IsGenericType)
                {
                    Type genericArg = type.GetGenericArguments()[0];
                    return genericArg.FullName?.Replace('+', '.') + "[]";
                }
                return type.FullName?.Replace('+', '.');
            };
        });
    }

    private static void AddAuthenticationLayer(this WebApplicationBuilder builder)
    {
        IConfigurationSection authSection = builder.Configuration.GetSection("Authentication");

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            })
            .AddOpenIdConnect(options =>
            {
                IConfigurationSection auth = builder.Configuration.GetSection("Authentication");

                options.MetadataAddress = auth["MetadataAddress"];
                options.ClientId = auth["ClientId"];
                options.ClientSecret = auth["ClientSecret"];
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.CallbackPath = auth["CallbackPath"];
                options.SignedOutCallbackPath = auth["SignedOutCallbackPath"];
                options.RemoteSignOutPath = auth["RemoteSignOutPath"];

                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.UseIfAvailable;

                options.Scope.Clear();
                foreach (string scope in auth.GetSection("Scope").Get<string[]>() ?? [])
                {
                    options.Scope.Add(scope);
                }

                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = async context =>
                    {
                        string email = context.Principal?.FindFirst("email")?.Value ?? "";
                        if (string.IsNullOrWhiteSpace(email))
                        {
                            return;
                        }

                        AppDbContext dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                        bool employeeExists = await dbContext.Employees.AnyAsync(e => e.Email == email, context.HttpContext.RequestAborted);
                        if (employeeExists)
                        {
                            return;
                        }

                        static int? ExtractPersonalNumber(ClaimsPrincipal? principal)
                        {
                            // Example: "urn:schac:personalUniqueCode:int:esi:ujep.cz:105976"
                            string? schac = principal?.FindFirst("schacPersonalUniqueCode")?.Value;
                            if (string.IsNullOrWhiteSpace(schac))
                            {
                                return null;
                            }
                            ReadOnlySpan<char> last = schac.AsSpan(schac.LastIndexOf(':') + 1);
                            return int.TryParse(last, out int value) ? value : null;
                        }

                        Employee employee = new()
                        {
                            Id = Guid.NewGuid(),
                            FullName = context.Principal?.FindFirst("displayName")?.Value ?? "",
                            Email = email,
                            IsGlobalManager = false,
                            EmployeeTypeId = null,
                            PersonalNumber = ExtractPersonalNumber(context.Principal),
                            CreatedAt = DateTime.UtcNow
                        };
                        dbContext.Employees.Add(employee);
                        await dbContext.SaveChangesAsync(context.HttpContext.RequestAborted);
                    }
                };
            });

        builder.Services.AddAuthorization();
    }

    private static void AddDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<AppDbContext>();
    }

    private static void AddAppServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ICellParser, CellParser>();
        builder.Services.AddSingleton<ITimesheetReader<AttendanceTimesheet>, AttendanceTimesheetReader>();
        builder.Services.AddSingleton<IPublicHolidayProvider, CzechPublicHolidayProvider>();
        builder.Services.AddTransient<ITimesheetImporter<AttendanceTimesheet>, AttendanceTimesheetImporter>();
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();
        builder.Services.AddSignalR();
        builder.Services.AddScoped<NotificationSender>();
    }
}
