using Timesheets.Api.Features.Attendance.Endpoints;
using Timesheets.Api.Features.Auth;
using Timesheets.Api.Features.Auth.Endpoints;
using Timesheets.Api.Features.Contracts.Endpoints;
using Timesheets.Api.Features.Employees.Endpoints;
using Timesheets.Api.Features.Notifications;
using Timesheets.Api.Features.Notifications.Endpoints;
using Timesheets.Api.Features.Projects.Endpoints;
using Timesheets.Api.Features.Timesheets.Endpoints;

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
            endpoints.MapEndpoint<EmployeeOnly>();
        }

        endpoints.MapEndpoint<GetCurrentUser>();
    }

    private static void MapProtectedApiEndpoints(this WebApplication app)
    {
        var endpoints = app.MapGroup("/api").AddEndpointFilter<EnsureCurrentUserLoadedFilter>();

        if (AuthenticationConfig.IsEnabled(app.Configuration))
        {
            endpoints.RequireAuthorization();
        }

        endpoints.MapProjectEndpoints();
        endpoints.MapContractEndpoints();
        endpoints.MapEmployeeEndpoints();
        endpoints.MapAttendanceEndpoints();
        endpoints.MapTimesheetEndpoints();
        endpoints.MapNotificationEndpoints();
    }

    private static void MapProjectEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGroup("/projects").WithTags("Projects")
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
        .MapEndpoint<GetProjectManagers>()
        .MapEndpoint<GetProjectContractsManagers>()
        .MapEndpoint<AddProjectManager>()
        .MapEndpoint<RemoveProjectManager>()
        .MapEndpoint<CreateProjectContract>()
        .MapEndpoint<UpdateProjectContract>()
        .MapEndpoint<DeleteProjectContract>();

    private static void MapContractEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGroup("/contracts").WithTags("Contracts")
        .MapEndpoint<GetContractCatalog>()
        .MapEndpoint<GetContractTimesheetsFilterOptions>()
        .MapEndpoint<GetContractTimesheets>()
        .MapEndpoint<GetContractEmployees>()
        .MapEndpoint<GetContractEmployeeAddImpact>()
        .MapEndpoint<AddContractEmployee>()
        .MapEndpoint<GetContractEmployeeUpdateImpact>()
        .MapEndpoint<UpdateContractEmployee>()
        .MapEndpoint<RemoveContractEmployee>()
        .MapEndpoint<AddContractManager>()
        .MapEndpoint<RemoveContractManager>();

    private static void MapAttendanceEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGroup("/attendance").WithTags("Attendance")
        .MapEndpoint<DetectAttendance>()
        .MapEndpoint<ImportAttendance>();

    private static void MapTimesheetEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGroup("/timesheets").WithTags("Timesheets")
        .MapEndpoint<GetTimesheetOverview>()
        .MapEndpoint<GetTimesheet>()
        .MapEndpoint<UpdateTimesheet>()
        .MapEndpoint<AllocateTimesheet>()
        .MapEndpoint<UpdateTimesheetStatus>()
        .MapEndpoint<GetTimesheetComments>()
        .MapEndpoint<AddTimesheetComment>()
        .MapEndpoint<DeleteTimesheetComment>()
        .MapEndpoint<ReviewTimesheet>();

    private static void MapEmployeeEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGroup("/employees").WithTags("Employees")
        .MapEndpoint<GetEmployee>()
        .MapEndpoint<UpdateEmployeeGlobalManagerRole>()
        .MapEndpoint<GetEmployees>()
        .MapEndpoint<GetEmployeePositions>()
        .MapEndpoint<GetEmployeeTimesheets>();

    private static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup("/notifications").WithTags("Notifications");
        endpoints.MapEndpoint<GetEmployeeNotifications>();
        endpoints.MapEndpoint<MarkNotificationAsRead>();
        endpoints.MapEndpoint<MarkAllNotificationsAsRead>();
        app.MapHub<NotificationHub>("/notifications/hub");
    }

    private static IEndpointRouteBuilder MapEndpoint<TEndpoint>(this IEndpointRouteBuilder app) where TEndpoint : IEndpoint
    {
        TEndpoint.Map(app);
        return app;
    }
}
