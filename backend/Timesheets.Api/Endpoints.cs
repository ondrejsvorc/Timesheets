using Timesheets.Api.Contracts.Endpoints;
using Timesheets.Api.Employees.Endpoints;
using Timesheets.Api.Notifications;
using Timesheets.Api.Notifications.Endpoints;
using Timesheets.Api.Projects.Endpoints;
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
        var endpoints = app.MapGroup("/api");
        endpoints.MapProjectEndpoints();
        endpoints.MapContractEndpoints();
        endpoints.MapEmployeeEndpoints();
        endpoints.MapTimesheetEndpoints();
        endpoints.MapNotificationEndpoints();
    }

    private static void MapProjectEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGroup("/projects").WithTags("Projects")
        .MapEndpoint<GetProjectContracts>()
        .MapEndpoint<UpdateProject>()
        .MapEndpoint<DeleteProject>()
        .MapEndpoint<GetProjects>()
        .MapEndpoint<GetProjectCatalog>()
        .MapEndpoint<CreateProject>()
        .MapEndpoint<CreateProjectContract>();

    private static void MapContractEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGroup("/contracts").WithTags("Contracts")
        .MapEndpoint<GetContract>()
        .MapEndpoint<GetContractCatalog>()
        .MapEndpoint<UpdateContract>()
        .MapEndpoint<DeleteContract>()
        .MapEndpoint<GetContractEmployees>()
        .MapEndpoint<AddContractEmployee>()
        .MapEndpoint<UpdateContractEmployee>()
        .MapEndpoint<RemoveContractEmployee>()
        .MapEndpoint<GetContractManagers>()
        .MapEndpoint<AddContractManager>()
        .MapEndpoint<RemoveContractManager>();

    private static void MapTimesheetEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGroup("/timesheets").WithTags("Timesheets")
        .MapEndpoint<ImportAttendanceTimesheet>();

    private static void MapEmployeeEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGroup("/employees").WithTags("Employees")
        .MapEndpoint<GetEmployee>()
        .MapEndpoint<UpdateEmployee>()
        .MapEndpoint<DeleteEmployee>()
        .MapEndpoint<GetEmployees>()
        .MapEndpoint<CreateEmployee>();

    private static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup("/notifications").WithTags("Notifications");
        endpoints.MapEndpoint<GetEmployeeNotifications>();
        endpoints.MapEndpoint<MarkNotificationAsRead>();
        app.MapHub<NotificationHub>("/notifications/hub");
    }

    private static IEndpointRouteBuilder MapEndpoint<TEndpoint>(this IEndpointRouteBuilder app) where TEndpoint : IEndpoint
    {
        TEndpoint.Map(app);
        return app;
    }
}