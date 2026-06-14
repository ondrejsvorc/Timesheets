using Timesheets.Api.Auth;
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
        app.MapAuthEndpoints();
        app.MapProtectedApiEndpoints();
    }

    private static void MapAuthEndpoints(this WebApplication app)
    {
        var endpoints = app.MapGroup("/auth").AllowAnonymous().WithTags("Authentication");

        if (AuthenticationConfig.IsEnabled(app.Configuration))
        {
            endpoints.MapEndpoint<Login>();
            endpoints.MapEndpoint<Logout>();
        }

        endpoints.MapEndpoint<GetCurrentUser>();
        endpoints.MapEndpoint<GetCurrentUserPermissions>();
    }

    private static void MapProtectedApiEndpoints(this WebApplication app)
    {
        var endpoints = app.MapGroup("/api")
            .AddEndpointFilter<EnsureCurrentUserLoadedFilter>();
        if (AuthenticationConfig.IsEnabled(app.Configuration))
        {
            endpoints.RequireAuthorization();
        }
        endpoints.MapProjectEndpoints();
        endpoints.MapContractEndpoints();
        endpoints.MapEmployeeEndpoints();
        endpoints.MapTimesheetEndpoints();
        endpoints.MapNotificationEndpoints();
    }

    private static void MapProjectEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGroup("/projects").WithTags("Projects")
        .MapEndpoint<GetProjectDeleteImpact>()
        .MapEndpoint<GetProject>()
        .MapEndpoint<UpdateProject>()
        .MapEndpoint<ArchiveProject>()
        .MapEndpoint<UnarchiveProject>()
        .MapEndpoint<DeleteProject>()
        .MapEndpoint<GetProjects>()
        .MapEndpoint<GetProjectCatalog>()
        .MapEndpoint<CreateProject>()
        .MapEndpoint<GetProjectContract>()
        .MapEndpoint<GetProjectContracts>()
        .MapEndpoint<GetProjectContractsManagers>()
        .MapEndpoint<CreateProjectContract>()
        .MapEndpoint<UpdateProjectContract>()
        .MapEndpoint<DeleteProjectContract>();

    private static void MapContractEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGroup("/contracts").WithTags("Contracts")
        .MapEndpoint<GetContractDeleteImpact>()
        .MapEndpoint<GetContractCatalog>()
        .MapEndpoint<GetContractTimesheetsFilterOptions>()
        .MapEndpoint<GetContractTimesheets>()
        .MapEndpoint<GetContractEmployees>()
        .MapEndpoint<AddContractEmployee>()
        .MapEndpoint<GetContractEmployeeUpdateImpact>()
        .MapEndpoint<UpdateContractEmployee>()
        .MapEndpoint<RemoveContractEmployee>()
        .MapEndpoint<AddContractManager>()
        .MapEndpoint<RemoveContractManager>();

    private static void MapTimesheetEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGroup("/timesheets").WithTags("Timesheets")
        .MapEndpoint<GetCombinedTimesheetOverview>()
        .MapEndpoint<GetCombinedTimesheet>()
        .MapEndpoint<GetTimesheetCatalog>()
        .MapEndpoint<GetTimesheetStatuses>()
        .MapEndpoint<UpdateTimesheet>()
        .MapEndpoint<UpdateCombinedTimesheetStatus>()
        .MapEndpoint<GetTimesheetComments>()
        .MapEndpoint<AddTimesheetComment>()
        .MapEndpoint<ReviewTimesheet>()
        .MapEndpoint<DetectTimesheetImport>()
        .MapEndpoint<ImportTimesheet>();

    private static void MapEmployeeEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGroup("/employees").WithTags("Employees")
        .MapEndpoint<GetEmployee>()
        .MapEndpoint<UpdateEmployeeGlobalManagerRole>()
        .MapEndpoint<UpdateEmployeeType>()
        .MapEndpoint<GetEmployees>()
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
