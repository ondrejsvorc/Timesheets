using ClosedXML.Excel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Timesheets.Api.Timesheets;

public interface ITimesheetReader<T> where T : ITimesheet
{
    T Read(Stream stream);
}

public abstract class TimesheetReaderBase
{
    protected static readonly CultureInfo CzechCulture = new("cs-CZ");

    protected static TimeOnly? ParseTime(IXLCell cell)
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
        string text = cell.GetString().Trim();
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

    protected static string? ParseString(string? value)
    {
        string? trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    protected static double? ParseDouble(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.TryGetValue(out double numValue))
        {
            return numValue;
        }

        string? text = cell.GetString()?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        // Nahradit čárku za tečku pro parsování
        text = text.Replace(',', '.');
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
        {
            return parsed;
        }

        return null;
    }

    protected static decimal? ParseDecimal(IXLCell cell)
    {
        double? value = ParseDouble(cell);
        return value.HasValue ? (decimal)value.Value : null;
    }
}

public sealed partial class AttendanceTimesheetReader : TimesheetReaderBase, ITimesheetReader<AttendanceTimesheet>
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
                ClockIn: ParseTime(sheet.Cell($"B{rowNum}")),
                ClockOut: ParseTime(sheet.Cell($"C{rowNum}")),
                BreakStart: ParseTime(sheet.Cell($"D{rowNum}")),
                BreakEnd: ParseTime(sheet.Cell($"E{rowNum}")),
                OtherInterruption: ParseString(sheet.Cell($"F{rowNum}").GetString()),
                HoursWithoutBreak: ParseDecimal(sheet.Cell($"G{rowNum}")),
                HoursObligation: ParseDecimal(sheet.Cell($"H{rowNum}")),
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

public sealed partial class ProjectTimesheetReader : TimesheetReaderBase, ITimesheetReader<ProjectTimesheet>
{
    [GeneratedRegex(@"^(\d{2})/(\d{4})$")]
    private static partial Regex PeriodRegex();

    public ProjectTimesheet Read(Stream stream)
    {
        using XLWorkbook workbook = new(stream);
        IXLWorksheet sheet = workbook.Worksheets.Worksheet(1);

        // Název projektu
        string? projectName = ParseString(sheet.Cell("A4").GetString());

        // Název příjemce
        string? recipientName = ParseString(sheet.Cell("G4").GetString());

        // Registrační číslo projektu 
        string? projectRegistrationNumber = ParseString(sheet.Cell("K4").GetString());

        // Celý název zaměstnance včetně titulů
        string? employeeName = ParseString(sheet.Cell("D7").GetString());


        // Název pozice
        string? positionName = ParseString(sheet.Cell("K7").GetString());

        // Výše úvazku u zaměstnavatele
        decimal? workloadPercent = ParseDecimal(sheet.Cell("D9"));

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
            string? activityKey = ParseString(sheet.Cell($"B{rowNum}").GetString());

            // Název skupiny činností
            string? activityGroup = ParseString(sheet.Cell($"C{rowNum}").GetString());

            // Popis činností
            string? description = ParseString(sheet.Cell($"D{rowNum}").GetString());

            // Počet hodin
            decimal? hours = ParseDecimal(sheet.Cell($"O{rowNum}"));

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
