using Timesheets.Api.Timesheets.Endpoints;

namespace Timesheets.Api;

public interface IEndpoint
{
    static abstract void Map(IEndpointRouteBuilder app);
}

public static class Endpoints
{
    public static void MapEndpoints(this WebApplication app)
    {
        var endpoints = app.MapGroup("/api").WithOpenApi();
        endpoints.MapTimesheetEndpoints();
    }

    private static void MapTimesheetEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup("/timesheets")
            .WithTags("Timesheets");

        endpoints
            .MapEndpoint<ImportAttendanceTimesheet>();
    }

    private static IEndpointRouteBuilder MapEndpoint<TEndpoint>(this IEndpointRouteBuilder app) where TEndpoint : IEndpoint
    {
        TEndpoint.Map(app);
        return app;
    }
}