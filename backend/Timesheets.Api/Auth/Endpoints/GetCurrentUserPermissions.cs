using Timesheets.Api.Auth;
using Timesheets.Api.Common;
using Timesheets.Api.Common.Extensions;

namespace Timesheets.Api.Auth.Endpoints;

public sealed class GetCurrentUserPermissions : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/currentUserPermissions", Handle)
           .WithSummary("Get Currently Authenticated User Permissions")
           .AddEndpointFilter(RequireAuthenticatedFilter.Instance)
           .AddEndpointFilter<EnsureCurrentUserLoadedFilter>();

    public sealed record Response(
        UserRole Role,
        IReadOnlyList<Guid> ProjectManagerOf,
        IReadOnlyList<Guid> ContractManagerOf,
        IReadOnlyList<Guid> EmployeeOnContractIds,
        IReadOnlyList<Guid> VisibleProjectIds,
        IReadOnlyList<Guid> VisibleContractIds);

    private static Task<IResult> Handle(ICurrentUser user) =>
        Task.FromResult<IResult>(Results.Ok(new Response(
            Role: user.Role,
            ProjectManagerOf: user.ProjectManagerOf,
            ContractManagerOf: user.ContractManagerOf,
            EmployeeOnContractIds: user.EmployeeOnContractIds,
            VisibleProjectIds: user.VisibleProjectIds,
            VisibleContractIds: user.VisibleContractIds)));

    private sealed class RequireAuthenticatedFilter : IEndpointFilter
    {
        public static readonly RequireAuthenticatedFilter Instance = new();

        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) =>
            context.HttpContext.User.IsAuthenticated()
                ? next(context)
                : ValueTask.FromResult<object?>(Results.Unauthorized());
    }
}
