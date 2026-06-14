using System.Text.Json;
using Microsoft.AspNetCore.Http.Extensions;

namespace Timesheets.Api.Auth;

internal sealed class OidcCallbackIframeBreakoutMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (IsOidcCallbackPath(context.Request.Path) && IsIframeRequest(context.Request))
        {
            string target = context.Request.GetEncodedPathAndQuery();
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.Headers.CacheControl = "no-store";
            await context.Response.WriteAsync(
                $$"""
                <!DOCTYPE html>
                <html lang="cs">
                <head><meta charset="utf-8"><title>Přihlášení…</title></head>
                <body>
                <script>
                (function () {
                  var target = {{JsonSerializer.Serialize(target)}};
                  var url = target.startsWith("http") ? target : (window.location.origin + target);
                  try { window.top.location.replace(url); } catch (e) { window.location.replace(url); }
                })();
                </script>
                </body>
                </html>
                """);
            return;
        }

        await next(context);
    }

    private static bool IsOidcCallbackPath(PathString path) =>
        path.StartsWithSegments("/login-oidc") || path.StartsWithSegments("/logout-oidc");

    private static bool IsIframeRequest(HttpRequest request) =>
        string.Equals(request.Headers["Sec-Fetch-Dest"].ToString(), "iframe", StringComparison.OrdinalIgnoreCase);
}
