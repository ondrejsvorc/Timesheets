using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Projects;

public class DeleteProjectTests : BaseIntegrationTest
{
    public DeleteProjectTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task DeleteProject_WithNonExistentId_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();
        var response = await Client.DeleteAsync($"/api/projects/{nonExistentId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
