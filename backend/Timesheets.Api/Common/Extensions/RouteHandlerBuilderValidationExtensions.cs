using Timesheets.Api.Common.Filters;

namespace Timesheets.Api.Common.Extensions;

public static class RouteHandlerBuilderValidationExtensions
{
    /// <summary>
    /// Adds a request validation filter to the route handler.
    /// </summary>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to futher customize the endpoint.</returns>
    public static RouteHandlerBuilder WithRequestValidation<TRequest>(this RouteHandlerBuilder builder)
    {
        return builder
            .AddEndpointFilter<RequestValidationFilter<TRequest>>()
            .ProducesValidationProblem();
    }
}
