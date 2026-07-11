using Timesheets.Api.Features.Timesheets;

namespace Timesheets.Api.Tests.Unit;

public sealed class TimesheetEditRequestValidatorTests
{
    private readonly TimesheetEditRequestValidator _validator = new();

    [Fact]
    public void Allows_one_non_half_hour_project_cell()
    {
        TimesheetEditRequest request = Request(1m, 0.4m, 2m);

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Rejects_two_non_half_hour_project_cells()
    {
        TimesheetEditRequest request = Request(1m, 0.4m, 2.7m);

        Assert.False(_validator.Validate(request).IsValid);
    }

    private static TimesheetEditRequest Request(params decimal[] projectHours)
    {
        DateTime start = new(2064, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        TimesheetDayEdit[] days = projectHours
            .Select((_, index) => new TimesheetDayEdit(start.AddDays(index), null, null, null, null, 0m, null, []))
            .ToArray();
        ContractPartDayEdit[] contractPartDays = projectHours
            .Select((hours, index) => new ContractPartDayEdit(start.AddDays(index), hours))
            .ToArray();
        return new TimesheetEditRequest(days, [new ContractPartEdit(Guid.CreateVersion7(), contractPartDays)]);
    }
}
