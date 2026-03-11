using ClosedXML.Excel;
using System.Globalization;

namespace Timesheets.Api.Timesheets;

public interface ITimesheetReader<T> where T : ITimesheet
{
    T Read(Stream stream);
}

public sealed class AttendanceTimesheetReader(ICellParser cellParser) : ITimesheetReader<AttendanceTimesheet>
{
    public AttendanceTimesheet Read(Stream stream)
    {
        using XLWorkbook workbook = new(stream);
        IXLWorksheet sheet = workbook.Worksheets.Worksheet(1);
        AttendanceTimesheetMetadata metadata = AttendanceTimesheetWorksheetParser.ReadMetadata(sheet);

        // Načtení řádků - od řádku 4 do (4 + daysInMonth - 1)
        List<AttendanceDay> rows = [];
        const int headerOffset = 3;

        for (int i = 0; i < metadata.DaysInMonth; i++)
        {
            int rowNum = headerOffset + 1 + i;

            var row = new AttendanceDay
            (
                Date: new DateTime(metadata.Year, metadata.Month, i + 1),
                ClockIn: cellParser.ParseTime(sheet.Cell($"B{rowNum}")),
                ClockOut: cellParser.ParseTime(sheet.Cell($"C{rowNum}")),
                BreakStart: cellParser.ParseTime(sheet.Cell($"D{rowNum}")),
                BreakEnd: cellParser.ParseTime(sheet.Cell($"E{rowNum}")),
                OtherInterruption: cellParser.ParseString(sheet.Cell($"F{rowNum}")),
                Schedules: cellParser.ParseTimeRanges(sheet.Cell($"K{rowNum}")),
                IsHoliday: false,
                Workload: metadata.Workload
            );

            rows.Add(row);
        }

        return new AttendanceTimesheet
        (
            EmployeePersonalNumber: metadata.EmployeePersonalNumber,
            EmployeeName: metadata.EmployeeName,
            Workload: metadata.Workload,
            Year: metadata.Year,
            Month: metadata.Month,
            Days: rows
        );
    }
}

public interface ICellParser
{
    TimeSpan? ParseTime(IXLCell cell);
    string? ParseString(IXLCell cell);
    decimal? ParseDecimal(IXLCell cell);
    IReadOnlyList<TimeRange> ParseTimeRanges(IXLCell cell);
}

public sealed class CellParser : ICellParser
{
    private static readonly string[] AllowedTimeFormats =
    [
        "h\\:mm",
        "hh\\:mm",
        "h\\:mm\\:ss",
        "hh\\:mm\\:ss"
    ];

    public TimeSpan? ParseTime(IXLCell cell)
    {
        string? text = ParseString(cell);
        if (text is null)
        {
            return null;
        }
        if (TimeSpan.TryParseExact(text, AllowedTimeFormats, CultureInfo.InvariantCulture, out TimeSpan parsed))
        {
            return parsed;
        }
        return null;
    }

    public decimal? ParseDecimal(IXLCell cell)
    {
        // V českém prostředí se používá čárka jako desetinný oddělovač (např. "8,5"),
        // ale InvariantCulture očekává tečku ("8.5"). Proto zde čárku převádíme na tečku,
        // aby parser fungoval konzistentně i v případě, že se v Excelu objeví smíšené zápisy.
        string? text = ParseString(cell)?.Replace(',', '.');
        if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal parsed))
        {
            return parsed;
        }
        return null;
    }

    public string? ParseString(IXLCell cell)
    {
        string text = cell.GetString().Trim();
        return text.Length == 0 ? null : text;
    }

    public IReadOnlyList<TimeRange> ParseTimeRanges(IXLCell cell)
    {
        string? text = ParseString(cell);
        if (text is null)
        {
            return [];
        }

        const char intervalSeparator = ',';
        const char rangeSeparator = '-';

        string[] parts = text.Split(intervalSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        List<TimeRange> ranges = new(parts.Length);
        
        foreach (string part in parts)
        {
            string[] tokens = part.Split(rangeSeparator, StringSplitOptions.TrimEntries);
            if (tokens.Length != 2)
            {
                continue;
            }

            if (!TimeSpan.TryParseExact(tokens[0], AllowedTimeFormats, CultureInfo.InvariantCulture, out TimeSpan start))
            {
                continue;
            }

            if (!TimeSpan.TryParseExact(tokens[1], AllowedTimeFormats, CultureInfo.InvariantCulture, out TimeSpan end))
            {
                continue;
            }

            ranges.Add(new TimeRange(start, end));
        }

        return ranges;
    }
}