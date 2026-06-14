using System.Net;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Projects;

public class GetProjectTests : BaseIntegrationTest
{
    public GetProjectTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetProject_WithNonExistentId_ReturnsNotFound()
    {
        Guid nonExistentId = Guid.NewGuid();
        HttpResponseMessage response = await Client.GetAsync($"/api/projects/{nonExistentId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
