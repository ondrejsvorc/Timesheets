using System.IO.Compression;
using CzechHolidays;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Timesheets.Api.Auth;
using Timesheets.Api.Data;
using Timesheets.Api.Notifications;
using Timesheets.Api.Timesheets;

namespace Timesheets.Api;

public static class ConfigureServices
{
    public static void AddServices(this WebApplicationBuilder builder)
    {
        builder.AddOpenApi();
        builder.AddForwardedHeaders();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod();
            });
        });
        builder.Services.AddHealthChecks();
        builder.AddResponseCompression();
        builder.AddAuthentication();
        builder.AddDatabase();
        builder.AddAppServices();
    }

    private static void AddForwardedHeaders(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }

    private static void AddResponseCompression(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        builder.Services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });
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

    private static void AddAuthentication(this WebApplicationBuilder builder)
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
                options.SignedOutRedirectUri = auth["SignedOutRedirectUri"] ?? "/error";

                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;

                options.Scope.Clear();
                foreach (string scope in auth.GetSection("Scope").Get<string[]>() ?? [])
                {
                    options.Scope.Add(scope);
                }

                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProviderForSignOut = context =>
                    {
                        // We want the IdP to redirect straight to the SPA route (e.g. /login),
                        // not to the middleware callback path.
                        string? signedOutRedirectUri = auth["SignedOutRedirectUri"];
                        if (!string.IsNullOrWhiteSpace(signedOutRedirectUri))
                        {
                            if (Uri.TryCreate(signedOutRedirectUri, UriKind.Absolute, out Uri? absolute))
                            {
                                context.ProtocolMessage.PostLogoutRedirectUri = absolute.ToString();
                                return Task.CompletedTask;
                            }

                            if (!signedOutRedirectUri.StartsWith('/'))
                            {
                                signedOutRedirectUri = "/" + signedOutRedirectUri;
                            }

                            context.ProtocolMessage.PostLogoutRedirectUri = UriHelper.BuildAbsolute(
                                context.Request.Scheme,
                                context.Request.Host,
                                context.Request.PathBase,
                                signedOutRedirectUri
                            );
                        }

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        if (context.Principal is null)
                        {
                            throw new InvalidOperationException("OIDC Principal is missing.");
                        }

                        UserSynchronizer synchronizer = context.HttpContext.RequestServices.GetRequiredService<UserSynchronizer>();
                        await synchronizer.SyncFromPrincipalAsync(context.Principal, context.HttpContext.RequestAborted);
                    }
                };
            });

        builder.Services.AddAuthorization();
    }

    private static void AddDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));
    }

    private static void AddAppServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<UserSynchronizer>();
        builder.Services.AddSingleton<ICellParser, CellParser>();
        builder.Services.AddSingleton<IAttendanceTimesheetMetadataReader, AttendanceTimesheetMetadataReader>();
        builder.Services.AddSingleton<ITimesheetReader<AttendanceTimesheet>, AttendanceTimesheetReader>();
        builder.Services.AddSingleton<ICzechHolidaysFactory, CzechHolidaysFactory>();
        builder.Services.AddTransient<ITimesheetImporter<AttendanceTimesheet>, AttendanceTimesheetImporter>();
        builder.Services.AddScoped<IAttendanceTimesheetPersistenceService, AttendanceTimesheetPersistenceService>();
        builder.Services.AddScoped<IAttendanceTimesheetImportService, AttendanceTimesheetImportService>();
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();
        builder.Services.AddSignalR();
        builder.Services.AddScoped<NotificationSender>();
    }
}
