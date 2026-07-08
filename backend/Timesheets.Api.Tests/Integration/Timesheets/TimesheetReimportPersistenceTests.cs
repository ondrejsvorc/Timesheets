using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Features.Timesheets;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Timesheets;

public class TimesheetReimportPersistenceTests : BaseIntegrationTest
{
    public TimesheetReimportPersistenceTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task PersistAsync_SecondImport_ReplacesDays()
    {
        TestProjectSetup setup = await IntegrationTestDataFactory.CreateProjectWithPositionAsync(Factory.Services, Client, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), workload: 0.5m);
        byte[] fileBytes = AttendanceTimesheetTestFileBuilder.Create(setup.EmployeePersonalNumber, "Test Employee", 2024, 10, 50m);

        using IServiceScope scope = CreateScope();
        AttendanceImport import = scope.ServiceProvider.GetRequiredService<AttendanceImport>();

        await using MemoryStream firstStream = new(fileBytes);
        AttendanceTimesheet firstImport = import.Read(firstStream);
        Guid firstTimesheetId = await import.PersistAsync(setup.EmployeeId, firstImport, CancellationToken.None);

        await using MemoryStream secondStream = new(fileBytes);
        AttendanceTimesheet secondImport = import.Read(secondStream);
        Guid secondTimesheetId = await import.PersistAsync(setup.EmployeeId, secondImport, CancellationToken.None);

        Assert.Equal(firstTimesheetId, secondTimesheetId);
    }
}
