using ClosedXML.Excel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Timesheets.Api.Timesheets;

public interface ITimesheetReader<T> where T : ITimesheet
{
    T Read(Stream stream);
}

public sealed partial class AttendanceTimesheetReader(ICellParser cellParser) : ITimesheetReader<AttendanceTimesheet>
{
    [GeneratedRegex(@"^(\d+)\s+(.+)$")]
    private static partial Regex EmployeeRegex();

    [GeneratedRegex(@"(\d{2})\.(\d{2})\.(\d{4})")]
    private static partial Regex PeriodRegex();

    [GeneratedRegex(@"(\d+)\s*%")]
    private static partial Regex WorkloadRegex();

    public AttendanceTimesheet Read(Stream stream)
    {
        using XLWorkbook workbook = new(stream);
        IXLWorksheet sheet = workbook.Worksheets.Worksheet(1);

        // Načtení kódu a jména zaměstnance z A1
        string cellA1 = sheet.Cell("A1").GetString();
        var employeeMatch = EmployeeRegex().Match(cellA1);

        int employeePersonalNumber = 0;
        string? employeeName = null;

        if (employeeMatch.Success)
        {
            employeePersonalNumber = int.Parse(employeeMatch.Groups[1].Value);
            employeeName = employeeMatch.Groups[2].Value.Trim();
        }

        // Načtení období a úvazku z A2
        string cellA2 = sheet.Cell("A2").GetString();
        var periodMatch = PeriodRegex().Match(cellA2);
        var workloadMatch = WorkloadRegex().Match(cellA2);

        int year = 0;
        int month = 0;
        int daysInMonth = 31;
        decimal workload = 1m;

        if (periodMatch.Success)
        {
            year = int.Parse(periodMatch.Groups[3].Value);
            month = int.Parse(periodMatch.Groups[2].Value);
            daysInMonth = DateTime.DaysInMonth(year, month);
        }

        if (workloadMatch.Success)
        {
            workload = decimal.Parse(workloadMatch.Groups[1].Value, CultureInfo.InvariantCulture) / 100m;
        }

        // Načtení řádků - od řádku 4 do (4 + daysInMonth - 1)
        List<AttendanceDay> rows = [];
        const int headerOffset = 3;

        for (int i = 0; i < daysInMonth; i++)
        {
            int rowNum = headerOffset + 1 + i;

            var row = new AttendanceDay
            (
                Date: new DateOnly(year, month, i + 1),
                ClockIn: cellParser.ParseTime(sheet.Cell($"B{rowNum}")),
                ClockOut: cellParser.ParseTime(sheet.Cell($"C{rowNum}")),
                BreakStart: cellParser.ParseTime(sheet.Cell($"D{rowNum}")),
                BreakEnd: cellParser.ParseTime(sheet.Cell($"E{rowNum}")),
                OtherInterruption: cellParser.ParseString(sheet.Cell($"F{rowNum}")),
                IsHoliday: false,
                Workload: workload
            );

            rows.Add(row);
        }

        return new AttendanceTimesheet
        (
            EmployeePersonalNumber: employeePersonalNumber,
            EmployeeName: employeeName,
            Workload: workload,
            Year: year,
            Month: month,
            Days: rows
        );
    }
}

public interface ICellParser
{
    TimeOnly? ParseTime(IXLCell cell);
    string? ParseString(IXLCell cell);
    decimal? ParseDecimal(IXLCell cell);
}

public sealed class CellParser : ICellParser
{
    private static readonly CultureInfo CzechCulture = new("cs-CZ");

    public TimeOnly? ParseTime(IXLCell cell)
    {
        string? text = ParseString(cell);
        if (TimeOnly.TryParseExact(text, ["H:mm", "HH:mm", "H:mm:ss", "HH:mm:ss"], CzechCulture, DateTimeStyles.None, out TimeOnly parsed))
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
}