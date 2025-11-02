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

    [GeneratedRegex(@"(\d{2})\.(\d{2})\.(\d{4})\s*-\s*(\d{2})\.(\d{2})\.(\d{4})")]
    private static partial Regex PeriodRegex();

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

        // Načtení období z A2
        string cellA2 = sheet.Cell("A2").GetString();

        var periodMatch = PeriodRegex().Match(cellA2);

        int year = 0;
        int month = 0;
        int daysInMonth = 31;

        if (periodMatch.Success)
        {
            int startDay = int.Parse(periodMatch.Groups[1].Value);
            int startMonth = int.Parse(periodMatch.Groups[2].Value);
            int startYear = int.Parse(periodMatch.Groups[3].Value);

            int endDay = int.Parse(periodMatch.Groups[4].Value);
            int endMonth = int.Parse(periodMatch.Groups[5].Value);
            int endYear = int.Parse(periodMatch.Groups[6].Value);

            // Předpokládáme, že období je vždy jeden měsíc
            year = startYear;
            month = startMonth;
            daysInMonth = DateTime.DaysInMonth(year, month);
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
                HoursWithoutBreak: cellParser.ParseDecimal(sheet.Cell($"G{rowNum}")),
                HoursObligation: cellParser.ParseDecimal(sheet.Cell($"H{rowNum}")),
                IsHoliday: false
            );

            rows.Add(row);
        }

        return new AttendanceTimesheet
        (
            EmployeePersonalNumber: employeePersonalNumber,
            EmployeeName: employeeName,
            Year: year,
            Month: month,
            Days: rows
        );
    }
}

public sealed partial class ProjectTimesheetReader(ICellParser cellParser) : ITimesheetReader<ProjectTimesheet>
{
    [GeneratedRegex(@"^(\d{2})/(\d{4})$")]
    private static partial Regex PeriodRegex();

    public ProjectTimesheet Read(Stream stream)
    {
        using XLWorkbook workbook = new(stream);
        IXLWorksheet sheet = workbook.Worksheets.Worksheet(1);

        // Název projektu
        string? projectName = cellParser.ParseString(sheet.Cell("A4"));

        // Název příjemce
        string? recipientName = cellParser.ParseString(sheet.Cell("G4"));

        // Registrační číslo projektu 
        string? projectRegistrationNumber = cellParser.ParseString(sheet.Cell("K4"));

        // Celý název zaměstnance včetně titulů
        string? employeeName = cellParser.ParseString(sheet.Cell("D7"));

        // Název pozice
        string? positionName = cellParser.ParseString(sheet.Cell("K7"));

        // Výše úvazku u zaměstnavatele
        decimal? workloadPercent = cellParser.ParseDecimal(sheet.Cell("D9"));

        // Vykazovaný měsíc a rok 
        // Formát: 07/2018
        string periodCell = sheet.Cell("K10").GetString();
        var periodMatch = PeriodRegex().Match(periodCell);

        int year = 0;
        int month = 0;
        int daysInMonth = 31;

        if (periodMatch.Success)
        {
            month = int.Parse(periodMatch.Groups[1].Value);
            year = int.Parse(periodMatch.Groups[2].Value);
            daysInMonth = DateTime.DaysInMonth(year, month);
        }

        // Načtení řádků - od řádku 14 do (14 + daysInMonth - 1)
        List<ProjectDay> rows = [];
        const int headerOffset = 13; // řádek 14 je index 13

        for (int i = 0; i < daysInMonth; i++)
        {
            int rowNum = headerOffset + 1 + i;

            // Klíčová aktivita 
            string? activityKey = cellParser.ParseString(sheet.Cell($"B{rowNum}"));

            // Název skupiny činností
            string? activityGroup = cellParser.ParseString(sheet.Cell($"C{rowNum}"));

            // Popis činností
            string? description = cellParser.ParseString(sheet.Cell($"D{rowNum}"));

            // Počet hodin
            decimal? hours = cellParser.ParseDecimal(sheet.Cell($"O{rowNum}"));

            var row = new ProjectDay
            (
                Date: new DateOnly(year, month, i + 1),
                ActivityKey: activityKey,
                ActivityGroup: activityGroup,
                Description: description,
                Hours: hours,
                IsHoliday: false
            );

            rows.Add(row);
        }

        return new ProjectTimesheet
        (
            EmployeeName: employeeName,
            Year: year,
            Month: month,
            ProjectName: projectName,
            RecipientName: recipientName,
            ProjectRegistrationNumber: projectRegistrationNumber,
            PositionName: positionName,
            WorkloadPercent: workloadPercent,
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
        if (cell.IsEmpty())
        {
            return null;
        }

        // Excel může ukládat čas jako DateTime
        if (cell.TryGetValue(out DateTime dateTime))
        {
            return TimeOnly.FromDateTime(dateTime);
        }

        // Nebo jako číslo (fraction of day)
        if (cell.TryGetValue(out double timeValue) && timeValue >= 0 && timeValue < 1)
        {
            return TimeOnly.FromTimeSpan(TimeSpan.FromDays(timeValue));
        }

        // Pokus o parsování textové hodnoty
        string? text = ParseString(cell);
        if (!string.IsNullOrWhiteSpace(text))
        {
            if (TimeOnly.TryParseExact(text, ["H:mm", "HH:mm", "H.mm", "HH.mm"],
                CzechCulture, DateTimeStyles.None, out TimeOnly time))
            {
                return time;
            }
        }

        return null;
    }

    public decimal? ParseDecimal(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.TryGetValue(out decimal numValue))
        {
            return numValue;
        }

        string? text = ParseString(cell);
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        // Nahradit čárku za tečku pro parsování
        text = text.Replace(',', '.');
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