using Timesheets.Api.Contracts.Endpoints;
using Timesheets.Api.Employees.Endpoints;
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
        endpoints.MapTimesheetEndpoints();
        endpoints.MapEmployeesEndpoints();
    }

    private static void MapProjectEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGroup("/projects").WithTags("Projects")
        .MapEndpoint<CreateProject>()
        .MapEndpoint<CreateProjectContract>()
        .MapEndpoint<DeleteProject>()
        .MapEndpoint<GetProject>()
        .MapEndpoint<GetProjectContracts>()
        .MapEndpoint<GetProjects>()
        .MapEndpoint<UpdateProject>();

    private static void MapContractEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGroup("/contracts").WithTags("Contracts")
        .MapEndpoint<GetContract>()
        .MapEndpoint<UpdateContract>()
        .MapEndpoint<DeleteContract>()
        .MapEndpoint<GetContractEmployees>()
        .MapEndpoint<AddContractEmployee>()
        .MapEndpoint<UpdateContractEmployee>()
        .MapEndpoint<RemoveContractEmployee>();

    private static void MapTimesheetEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGroup("/timesheets").WithTags("Timesheets")
        .MapEndpoint<ImportAttendanceTimesheet>()
        .MapEndpoint<ImportProjectTimesheet>();

    private static void MapEmployeesEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGroup("/employees").WithTags("Employees")
        .MapEndpoint<CreateEmployee>()
        .MapEndpoint<DeleteEmployee>()
        .MapEndpoint<GetEmployee>()
        .MapEndpoint<GetEmployees>()
        .MapEndpoint<UpdateEmployee>();

    private static IEndpointRouteBuilder MapEndpoint<TEndpoint>(this IEndpointRouteBuilder app) where TEndpoint : IEndpoint
    {
        TEndpoint.Map(app);
        return app;
    }
}