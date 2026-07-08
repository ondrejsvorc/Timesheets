using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Timesheets.Api.Features.Auth.Endpoints;

public sealed class Logout : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/logout", Handle)
           .WithSummary("Perform OIDC Log-Out and Log Out User.");

    private static async Task Handle(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
    }
}
