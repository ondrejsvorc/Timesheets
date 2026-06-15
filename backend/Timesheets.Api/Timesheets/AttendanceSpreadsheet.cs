using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace Timesheets.Api.Timesheets;

public sealed record AttendanceTimesheetMetadata(string EmployeePersonalNumber, string? EmployeeName, decimal Workload, int Year, int Month, int DaysInMonth);

public static partial class AttendanceSpreadsheet
{
    private static readonly string[] AllowedTimeFormats = ["h\\:mm", "hh\\:mm", "h\\:mm\\:ss", "hh\\:mm\\:ss"];

    [GeneratedRegex(@"^(\d+)\s+(.+)$")]
    private static partial Regex EmployeeRegex();

    [GeneratedRegex(@"(\d{2})\.(\d{2})\.(\d{4})")]
    private static partial Regex PeriodRegex();

    [GeneratedRegex(@"(\d+)\s*%")]
    private static partial Regex WorkloadRegex();

    public static AttendanceTimesheet Read(Stream stream)
    {
        using XLWorkbook workbook = new(stream);
        IXLWorksheet sheet = workbook.Worksheets.Worksheet(1);
        AttendanceTimesheetMetadata metadata = ReadMetadata(sheet);
        List<AttendanceDay> days = [];

        for (int index = 0; index < metadata.DaysInMonth; index++)
        {
            int row = index + 4;
            DateTime date = new(metadata.Year, metadata.Month, index + 1);
            TimeSpan? clockIn = ParseTime(sheet.Cell($"B{row}"));
            TimeSpan? clockOut = ParseTime(sheet.Cell($"C{row}"));
            TimeSpan? breakStart = ParseTime(sheet.Cell($"D{row}"));
            TimeSpan? breakEnd = ParseTime(sheet.Cell($"E{row}"));
            string? interruption = ParseString(sheet.Cell($"F{row}"));
            IReadOnlyList<TimeRange> schedules = ParseTimeRanges(sheet.Cell($"K{row}"));
            days.Add(new AttendanceDay(Date: date, ClockIn: clockIn, ClockOut: clockOut, BreakStart: breakStart, BreakEnd: breakEnd, OtherInterruption: interruption, Schedules: schedules, IsHoliday: false, Workload: metadata.Workload));
        }

        return new AttendanceTimesheet(EmployeePersonalNumber: metadata.EmployeePersonalNumber, EmployeeName: metadata.EmployeeName, Workload: metadata.Workload, Year: metadata.Year, Month: metadata.Month, Days: days);
    }

    public static AttendanceTimesheetMetadata ReadMetadata(Stream stream)
    {
        using XLWorkbook workbook = new(stream);
        return ReadMetadata(workbook.Worksheets.Worksheet(1));
    }

    public static TimeSpan? ParseTime(IXLCell cell)
    {
        string? text = ParseString(cell);
        return TimeSpan.TryParseExact(text, AllowedTimeFormats, CultureInfo.InvariantCulture, out TimeSpan parsed) ? parsed : null;
    }

    public static string? ParseString(IXLCell cell)
    {
        string text = cell.GetString().Trim();
        return text.Length == 0 ? null : text;
    }

    public static IReadOnlyList<TimeRange> ParseTimeRanges(IXLCell cell)
    {
        string? text = ParseString(cell);
        if (text is null)
        {
            return [];
        }

        List<TimeRange> ranges = [];
        foreach (string part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] values = part.Split('-', StringSplitOptions.TrimEntries);
            if (values.Length == 2
                && TimeSpan.TryParseExact(values[0], AllowedTimeFormats, CultureInfo.InvariantCulture, out TimeSpan start)
                && TimeSpan.TryParseExact(values[1], AllowedTimeFormats, CultureInfo.InvariantCulture, out TimeSpan end))
            {
                ranges.Add(new TimeRange(start, end));
            }
        }

        return ranges;
    }

    private static AttendanceTimesheetMetadata ReadMetadata(IXLWorksheet sheet)
    {
        Match employee = EmployeeRegex().Match(sheet.Cell("A1").GetString());
        Match period = PeriodRegex().Match(sheet.Cell("A2").GetString());
        Match workloadMatch = WorkloadRegex().Match(sheet.Cell("A2").GetString());

        string personalNumber = employee.Success ? employee.Groups[1].Value.Trim() : string.Empty;
        string? name = employee.Success ? employee.Groups[2].Value.Trim() : null;
        int year = period.Success ? int.Parse(period.Groups[3].Value) : 0;
        int month = period.Success ? int.Parse(period.Groups[2].Value) : 0;
        int daysInMonth = period.Success ? DateTime.DaysInMonth(year, month) : 31;
        decimal workload = workloadMatch.Success ? decimal.Parse(workloadMatch.Groups[1].Value, CultureInfo.InvariantCulture) / 100m : 1m;

        return new AttendanceTimesheetMetadata(EmployeePersonalNumber: personalNumber, EmployeeName: name, Workload: workload, Year: year, Month: month, DaysInMonth: daysInMonth);
    }
}
