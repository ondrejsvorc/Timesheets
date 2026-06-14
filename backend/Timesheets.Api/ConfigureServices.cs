using System.IO.Compression;
using System.Security.Claims;
using CzechHolidays;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Timesheets.Api.Administration;
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
                policy
                    .WithOrigins("http://localhost:3000")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
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
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
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
        if (!AuthenticationConfig.IsEnabled(builder.Configuration))
        {
            builder.Services.AddAuthorization();
            return;
        }

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                // Strict breaks common OIDC flows (cross-site POST back to callback).
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = context =>
                    {
                        if (context.Request.Path.StartsWithSegments("/api"))
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return Task.CompletedTask;
                        }

                        context.Response.Redirect(context.RedirectUri);
                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = context =>
                    {
                        if (context.Request.Path.StartsWithSegments("/api"))
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            return Task.CompletedTask;
                        }

                        context.Response.Redirect(context.RedirectUri);
                        return Task.CompletedTask;
                    },
                };
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

                // Ensure OIDC correlation/nonce cookies survive the IdP redirect/POST.
                options.CorrelationCookie.SameSite = SameSiteMode.None;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.NonceCookie.SameSite = SameSiteMode.None;
                options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;

                // Be explicit about claim mapping (IdP can return these either in id_token or from UserInfo).
                options.ClaimActions.Add(new JsonKeyClaimAction("eduPersonPrincipalName", ClaimValueTypes.String, "eduPersonPrincipalName"));
                options.ClaimActions.Add(new JsonKeyClaimAction("displayName", ClaimValueTypes.String, "displayName"));
                options.ClaimActions.Add(new JsonKeyClaimAction("displayName", ClaimValueTypes.String, "name"));
                options.ClaimActions.Add(new JsonKeyClaimAction("personalNumber", ClaimValueTypes.String, "personalNumber"));
                options.ClaimActions.Add(new JsonKeyClaimAction("personalNumber", ClaimValueTypes.String, "personal_number"));
                options.ClaimActions.Add(new JsonKeyClaimAction("title", ClaimValueTypes.String, "title"));
                options.ClaimActions.Add(new JsonKeyClaimAction("titleBefore", ClaimValueTypes.String, "titleBefore"));
                options.ClaimActions.Add(new JsonKeyClaimAction("titleAfter", ClaimValueTypes.String, "titleAfter"));

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
                        if (string.IsNullOrWhiteSpace(signedOutRedirectUri))
                        {
                            return Task.CompletedTask;
                        }

                        // "//host/path" parses as a file: URI in .NET; treat as HTTPS instead.
                        if (signedOutRedirectUri.StartsWith("//", StringComparison.Ordinal))
                        {
                            signedOutRedirectUri = $"{Uri.UriSchemeHttps}:{signedOutRedirectUri}";
                        }

                        if (Uri.TryCreate(signedOutRedirectUri, UriKind.Absolute, out Uri? absolute))
                        {
                            if (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps)
                            {
                                context.ProtocolMessage.PostLogoutRedirectUri = absolute.ToString();
                                return Task.CompletedTask;
                            }

                            // Misconfigured file: or other scheme — recover path if possible (e.g. file:///login -> /login).
                            if (absolute.Scheme == Uri.UriSchemeFile && absolute.LocalPath.StartsWith('/'))
                            {
                                signedOutRedirectUri = absolute.LocalPath;
                            }
                            else if (signedOutRedirectUri.Contains("://", StringComparison.Ordinal))
                            {
                                signedOutRedirectUri = "/login";
                            }
                        }

                        if (!signedOutRedirectUri.StartsWith('/'))
                        {
                            signedOutRedirectUri = "/" + signedOutRedirectUri;
                        }

                        context.ProtocolMessage.PostLogoutRedirectUri = UriHelper.BuildAbsolute(
                            context.Request.Scheme is "http" or "https" ? context.Request.Scheme : "https",
                            context.Request.Host,
                            context.Request.PathBase,
                            signedOutRedirectUri
                        );

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        if (context.Principal is null)
                        {
                            throw new InvalidOperationException("OIDC Principal is missing.");
                        }

                        // Sync after UserInfo claims are available (see OnTicketReceived).
                        await Task.CompletedTask;
                    },
                    OnTicketReceived = async context =>
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
        builder.Services.Configure<AdministrationOptions>(builder.Configuration.GetSection(AdministrationOptions.SectionName));
        builder.Services.AddScoped<UserSynchronizer>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, CurrentUser>();
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
