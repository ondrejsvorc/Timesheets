using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Data;
using Xunit;

namespace Timesheets.Api.Tests.Integration;

public class TestSetupTests : BaseIntegrationTest
{
    public TestSetupTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Database_IsMigrated_And_Accessible()
    {
        using IServiceScope scope = CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        bool canConnect = await dbContext.Database.CanConnectAsync();
        Assert.True(canConnect);
    }

    [Fact]
    public void AuthMock_ClientIsCreated() => Assert.NotNull(Client);
}
