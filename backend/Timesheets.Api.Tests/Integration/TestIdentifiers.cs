namespace Timesheets.Api.Tests.Integration;

internal static class TestIdentifiers
{
    public static string Project(int value) => $"12345 12 {value:0000} 01";
    public static string Contract(int value) => $"54321 10 {value:0000} 54";
    public static string Position(int value) => $"1.1.1.2.1.{value:00}";
}
