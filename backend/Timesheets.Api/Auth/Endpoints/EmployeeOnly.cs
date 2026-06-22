using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Timesheets.Api.Auth.Endpoints;

public sealed class EmployeeOnly : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/employee-only", async (HttpContext context) => await Handle(context))
           .WithSummary("Show Employee-Only Access Message");

    private static async Task<IResult> Handle(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        const string html = """
            <!doctype html>
            <html lang="cs">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta http-equiv="refresh" content="5;url=/auth/login">
              <title>Výkazy</title>
              <style>
                body { font-family: system-ui, sans-serif; min-height: 100vh; display: grid; place-items: center; margin: 0; padding: 24px; color: #111827; background: #f9fafb; }
                main { max-width: 520px; padding: 32px; border: 1px solid #e5e7eb; border-radius: 8px; background: white; }
                h1 { margin: 0 0 12px; font-size: 24px; }
                p { margin: 0 0 20px; color: #4b5563; }
                a { color: #0f766e; font-weight: 600; }
              </style>
            </head>
            <body>
              <main>
                <h1>Tato aplikace je určena pouze pro zaměstnance.</h1>
                <p>Za chvíli budete přesměrováni na přihlašovací stránku aplikace.</p>
                <a href="/auth/login">Přejít na přihlášení</a>
              </main>
            </body>
            </html>
            """;

        return Results.Content(html, "text/html; charset=utf-8");
    }
}
