using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Timesheets.Api.Common.Extensions;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class GetProjects : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/", Handle)
           .WithSummary("Get Projects")
           .WithRequestValidation<Request>();

    public sealed record Request;
    public sealed record ProjectItem(Guid Id, string Identifier, string Name, DateOnly Start, DateOnly End, int ContractCount);
    public sealed record Response(IEnumerable<ProjectItem> Projects);
    public sealed class Validator : AbstractValidator<Request> { }

    private static async Task<Ok<Response>> Handle(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

