namespace Timesheets.Api.Timesheets;

public sealed record PublicHoliday(DateOnly Date, string LocalName, string Name);

public interface IPublicHolidayProvider
{
    Task<IReadOnlyCollection<PublicHoliday>> GetPublicHolidaysAsync(int year);
}

public sealed class CzechPublicHolidayProvider : IPublicHolidayProvider
{
    private static readonly HttpClient _httpClient = new();

    public async Task<IReadOnlyCollection<PublicHoliday>> GetPublicHolidaysAsync(int year)
    {
        string url = $"https://date.nager.at/api/v3/PublicHolidays/{year}/CZ";
        return await _httpClient.GetFromJsonAsync<List<PublicHoliday>>(url) ?? [];
    }
}
