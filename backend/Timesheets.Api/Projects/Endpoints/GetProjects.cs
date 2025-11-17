using Microsoft.AspNetCore.Http.HttpResults;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class GetProjects : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/", Handle)
           .WithSummary("Get Projects");

    public sealed record ProjectItem(Guid Id, string RegistrationNumber, string Name, DateOnly StartDate, DateOnly EndDate, int ContractCount);
    public sealed record Response(IEnumerable<ProjectItem> Projects);

    private static async Task<Ok<Response>> Handle(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

