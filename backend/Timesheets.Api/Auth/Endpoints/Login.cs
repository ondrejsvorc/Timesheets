using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Timesheets.Api.Auth.Endpoints;

public sealed class Login : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/login", Handle)
           .WithSummary("Initiate OIDC Authentication Flow and Log In User");

    private static async Task Handle(HttpContext context)
    {
        await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "/" });
    }
}
