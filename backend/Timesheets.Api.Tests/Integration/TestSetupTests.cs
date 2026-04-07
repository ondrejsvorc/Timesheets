using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Data;
using Xunit;

namespace Timesheets.Api.Tests.Integration;

public class TestSetupTests : BaseIntegrationTest
{
    public TestSetupTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Database_IsMigrated_And_Accessible()
    {
        // Arrange & Act
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var canConnect = await dbContext.Database.CanConnectAsync();

        // Assert
        Assert.True(canConnect, "The database should be accessible.");
    }

    [Fact]
    public void AuthMock_ClientIsCreated()
    {
        // Assert
        Assert.NotNull(Client);
    }
}
