namespace Timesheets.Api.Features.Auth;

internal sealed class EnsureCurrentUserLoadedFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        CurrentUser currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>() as CurrentUser
            ?? throw new InvalidOperationException("ICurrentUser must be registered as CurrentUser.");

        await currentUser.EnsureLoadedAsync(context.HttpContext.RequestAborted);
        return await next(context);
    }
}
