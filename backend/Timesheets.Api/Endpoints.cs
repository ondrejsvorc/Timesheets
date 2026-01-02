using Timesheets.Api.Auth.Endpoints;
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
        //app.MapAuthEndpoints();
        app.MapProtectedApiEndpoints();
    }

    private static void MapAuthEndpoints(this WebApplication app) =>
        app.MapGroup("/auth").AllowAnonymous().WithTags("Authentication")
        .MapEndpoint<Login>()
        .MapEndpoint<Logout>()
        .MapEndpoint<GetCurrentUser>()
        .MapEndpoint<GetCurrentUserPermissions>();

    private static void MapProtectedApiEndpoints(this WebApplication app)
    {
        var endpoints = app.MapGroup("/api");//.RequireAuthorization();
        endpoints.MapProjectEndpoints();
        endpoints.MapContractEndpoints();
        endpoints.MapEmployeeEndpoints();
        endpoints.MapTimesheetEndpoints();
        endpoints.MapNotificationEndpoints();
    }

    private static void MapProjectEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGroup("/projects").WithTags("Projects")
        .MapEndpoint<GetProject>()
        .MapEndpoint<UpdateProject>()
        .MapEndpoint<DeleteProject>()
        .MapEndpoint<GetProjects>()
        .MapEndpoint<GetProjectCatalog>()
        .MapEndpoint<CreateProject>()
        .MapEndpoint<GetProjectContracts>()
        .MapEndpoint<GetProjectContractsManagers>()
        .MapEndpoint<CreateProjectContract>();

    private static void MapContractEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGroup("/contracts").WithTags("Contracts")
        .MapEndpoint<GetContractCatalog>()
        .MapEndpoint<UpdateContract>()
        .MapEndpoint<DeleteContract>()
        .MapEndpoint<GetContractTimesheets>()
        .MapEndpoint<GetContractEmployees>()
        .MapEndpoint<AddContractEmployee>()
        .MapEndpoint<UpdateContractEmployee>()
        .MapEndpoint<RemoveContractEmployee>()
        .MapEndpoint<GetContractManagers>()
        .MapEndpoint<AddContractManager>()
        .MapEndpoint<RemoveContractManager>();

    private static void MapTimesheetEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGroup("/timesheets").WithTags("Timesheets")
        .MapEndpoint<GetAttendanceTimesheet>()
        .MapEndpoint<CreateTimesheet>()
        .MapEndpoint<UpdateTimesheet>()
        .MapEndpoint<ReviewTimesheet>()
        .MapEndpoint<ImportTimesheet>();

    private static void MapEmployeeEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGroup("/employees").WithTags("Employees")
        .MapEndpoint<GetEmployee>()
        .MapEndpoint<UpdateEmployee>()
        .MapEndpoint<DeleteEmployee>()
        .MapEndpoint<GetEmployees>()
        .MapEndpoint<CreateEmployee>()
        .MapEndpoint<GetEmployeePositions>()
        .MapEndpoint<GetEmployeeTimesheets>();

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