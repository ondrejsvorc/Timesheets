using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Timesheets.Api.Features.Timesheets;

namespace Timesheets.Api.Features.Attendance;

public sealed record AttendanceFile(string EmployeePersonalNumber, string? EmployeeName, decimal Workload, int Year, int Month, IReadOnlyList<AttendanceFileDay> Days);
public sealed record AttendanceFileDay(DateTime Date, TimeSpan? ClockIn, TimeSpan? ClockOut, TimeSpan? BreakStart, TimeSpan? BreakEnd, string? OtherInterruption, IReadOnlyList<TimeRange> Schedules, bool IsHoliday, decimal Workload);
public sealed record AttendanceFileMetadata(string EmployeePersonalNumber, string? EmployeeName, decimal Workload, int Year, int Month, int DaysInMonth);

public sealed partial class AttendanceFileReader
{
    private static readonly string[] AllowedTimeFormats = ["h\\:mm", "hh\\:mm", "h\\:mm\\:ss", "hh\\:mm\\:ss"];

    [GeneratedRegex(@"^(\d+)\s+(.+)$")]
    private static partial Regex EmployeeRegex();

    [GeneratedRegex(@"(\d{2})\.(\d{2})\.(\d{4})")]
    private static partial Regex PeriodRegex();

    [GeneratedRegex(@"(\d+)\s*%")]
    private static partial Regex WorkloadRegex();

    [GeneratedRegex(@"<tr\b[^>]*>(.*?)</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex HtmlRowRegex();

    [GeneratedRegex(@"<t[dh]\b[^>]*>(.*?)</t[dh]>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex HtmlCellRegex();

    [GeneratedRegex(@"<h1\b[^>]*>(.*?)</h1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex HtmlHeadingRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    public AttendanceFile Read(Stream stream)
    {
        byte[] content = ReadAllBytes(stream);
        if (LooksLikeHtml(content))
        {
            return ReadHtml(content);
        }

        using MemoryStream workbookStream = new(content);
        return ReadWorkbook(workbookStream);
    }

    public AttendanceFileMetadata ReadMetadata(Stream stream)
    {
        byte[] content = ReadAllBytes(stream);
        if (LooksLikeHtml(content))
        {
            return ReadHtmlMetadata(DecodeHtml(content));
        }

        using MemoryStream workbookStream = new(content);
        using XLWorkbook workbook = new(workbookStream);
        return ReadMetadata(workbook.Worksheets.Worksheet(1));
    }

    public static TimeSpan? ParseTime(IXLCell cell) => ParseTimeText(ParseString(cell));

    public static string? ParseString(IXLCell cell)
    {
        string text = cell.GetString().Trim();
        return text.Length == 0 ? null : text;
    }

    public static IReadOnlyList<TimeRange> ParseTimeRanges(IXLCell cell) => ParseTimeRangesText(ParseString(cell));

    private static AttendanceFile ReadWorkbook(Stream stream)
    {
        using XLWorkbook workbook = new(stream);
        IXLWorksheet sheet = workbook.Worksheets.Worksheet(1);
        AttendanceFileMetadata metadata = ReadMetadata(sheet);
        List<AttendanceFileDay> days = [];

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
            days.Add(new AttendanceFileDay(date, clockIn, clockOut, breakStart, breakEnd, interruption, schedules, IsHoliday: false, metadata.Workload));
        }

        return new AttendanceFile(metadata.EmployeePersonalNumber, metadata.EmployeeName, metadata.Workload, metadata.Year, metadata.Month, days);
    }

    private static AttendanceFile ReadHtml(byte[] content)
    {
        string html = DecodeHtml(content);
        AttendanceFileMetadata metadata = ReadHtmlMetadata(html);
        List<string[]> rows = ReadHtmlRows(html).Where(row => row.Length > 1 && PeriodRegex().IsMatch(row[0])).ToList();
        List<AttendanceFileDay> days = [];

        for (int index = 0; index < metadata.DaysInMonth; index++)
        {
            string[] row = index < rows.Count ? rows[index] : [];
            DateTime date = new(metadata.Year, metadata.Month, index + 1);
            IReadOnlyList<TimeRange> schedules = ParseTimeRangesText(GetHtmlCell(row, 9));
            if (schedules.Count == 0)
            {
                schedules = ParseTimeRangesText(GetHtmlCell(row, 10));
            }

            days.Add(new AttendanceFileDay(
                date,
                ParseTimeText(GetHtmlCell(row, 1)),
                ParseTimeText(GetHtmlCell(row, 2)),
                ParseTimeText(GetHtmlCell(row, 3)),
                ParseTimeText(GetHtmlCell(row, 4)),
                GetHtmlCell(row, 5),
                schedules,
                IsHoliday: false,
                metadata.Workload));
        }

        return new AttendanceFile(metadata.EmployeePersonalNumber, metadata.EmployeeName, metadata.Workload, metadata.Year, metadata.Month, days);
    }

    private static TimeSpan? ParseTimeText(string? text) =>
        TimeSpan.TryParseExact(text, AllowedTimeFormats, CultureInfo.InvariantCulture, out TimeSpan parsed) ? parsed : null;

    private static IReadOnlyList<TimeRange> ParseTimeRangesText(string? text)
    {
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

    private static AttendanceFileMetadata ReadHtmlMetadata(string html)
    {
        string heading = HtmlHeadingRegex().Matches(html).Select(match => CleanHtmlText(match.Groups[1].Value)).FirstOrDefault(text => text.Length > 0) ?? string.Empty;
        string firstRow = string.Join(' ', ReadHtmlRows(html).FirstOrDefault() ?? []);
        string metadataText = $"{heading} {firstRow}";

        Match employee = EmployeeRegex().Match(heading);
        Match period = PeriodRegex().Match(metadataText);
        Match workloadMatch = WorkloadRegex().Match(metadataText);

        string personalNumber = employee.Success ? employee.Groups[1].Value.Trim() : string.Empty;
        string? name = employee.Success ? employee.Groups[2].Value.Trim() : null;
        int year = period.Success ? int.Parse(period.Groups[3].Value) : 0;
        int month = period.Success ? int.Parse(period.Groups[2].Value) : 0;
        int daysInMonth = period.Success && year > 0 && month is >= 1 and <= 12 ? DateTime.DaysInMonth(year, month) : 31;
        decimal workload = workloadMatch.Success ? decimal.Parse(workloadMatch.Groups[1].Value, CultureInfo.InvariantCulture) / 100m : 1m;

        return new AttendanceFileMetadata(personalNumber, name, workload, year, month, daysInMonth);
    }

    private static List<string[]> ReadHtmlRows(string html) =>
        HtmlRowRegex()
            .Matches(html)
            .Select(row => HtmlCellRegex().Matches(row.Groups[1].Value).Select(cell => CleanHtmlText(cell.Groups[1].Value)).ToArray())
            .Where(row => row.Length > 0)
            .ToList();

    private static string? GetHtmlCell(IReadOnlyList<string> row, int index) =>
        index < row.Count && row[index].Length > 0 ? row[index] : null;

    private static string CleanHtmlText(string html)
    {
        string withoutTags = HtmlTagRegex().Replace(html, " ");
        string decoded = WebUtility.HtmlDecode(withoutTags).Replace('\u00a0', ' ');
        return WhitespaceRegex().Replace(decoded, " ").Trim();
    }

    private static AttendanceFileMetadata ReadMetadata(IXLWorksheet sheet)
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

        return new AttendanceFileMetadata(personalNumber, name, workload, year, month, daysInMonth);
    }

    private static byte[] ReadAllBytes(Stream stream)
    {
        using MemoryStream memory = new();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static bool LooksLikeHtml(byte[] content)
    {
        int index = 0;
        if (content.Length >= 3 && content[0] == 0xef && content[1] == 0xbb && content[2] == 0xbf)
        {
            index = 3;
        }

        while (index < content.Length && char.IsWhiteSpace((char)content[index]))
        {
            index++;
        }

        if (index >= content.Length || content[index] != '<')
        {
            return false;
        }

        string head = Encoding.UTF8.GetString(content, index, Math.Min(512, content.Length - index));
        return head.Contains("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase)
            || head.Contains("<html", StringComparison.OrdinalIgnoreCase)
            || head.Contains("<table", StringComparison.OrdinalIgnoreCase);
    }

    private static string DecodeHtml(byte[] content) => Encoding.UTF8.GetString(content);
}
