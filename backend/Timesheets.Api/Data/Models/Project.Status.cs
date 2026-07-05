using System.ComponentModel.DataAnnotations.Schema;

namespace Timesheets.Api.Data.Models;

public sealed partial class Project
{
    [NotMapped]
    public string Status
    {
        get
        {
            if (ArchivedAt.HasValue)
            {
                return "archived";
            }

            DateOnly today = TodayPrague();
            if (today < DateOnly.FromDateTime(StartDate))
            {
                return "inactive";
            }

            if (EndDate.HasValue && today > DateOnly.FromDateTime(EndDate.Value))
            {
                return "inactive";
            }

            return "active";
        }
    }

    private static DateOnly TodayPrague()
    {
        TimeZoneInfo czechTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");
        DateTime localToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, czechTimeZone).Date;
        return DateOnly.FromDateTime(localToday);
    }
}
