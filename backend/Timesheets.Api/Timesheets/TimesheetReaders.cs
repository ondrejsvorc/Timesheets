namespace Timesheets.Api.Timesheets;
using ClosedXML.Excel;
using System.Globalization;
using System.Text.RegularExpressions; 
using System.IO;

public interface ITimesheetReader<T>
{
    T Read(Stream stream);
}

public sealed partial class AttendanceTimesheetReader : ITimesheetReader<AttendanceTimesheet>
{
    private static readonly CultureInfo CzechCulture = new("cs-CZ");

    [GeneratedRegex(@"^(\d+)\s+(.+)$")]
    private static partial Regex EmployeeRegex();

    [GeneratedRegex(@"(\d{2})\.(\d{2})\.(\d{4})\s*-\s*(\d{2})\.(\d{2})\.(\d{4})")]
    private static partial Regex PeriodRegex();

    [GeneratedRegex(@"^(\d{2})\.(\d{2})\.(\d{4})")]
    private static partial Regex DateRegex();
    
    public AttendanceTimesheet Read(Stream stream)
    {
        using XLWorkbook workbook = new(stream);
        IXLWorksheet sheet = workbook.Worksheets.Worksheet(1);

        // Načtení kódu a jména zaměstnance z A1
        string cellA1 = sheet.Cell("A1").GetString();
        
        var employeeMatch = EmployeeRegex().Match(cellA1);
        
        int employeeCode = 0;
        string? employeeName = null;
        
        if (employeeMatch.Success)
        {
            employeeCode = int.Parse(employeeMatch.Groups[1].Value);
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
        List<AttendanceTimesheetRow> rows = new();
        int headerOffset = 3;
        
        for (int i = 0; i < daysInMonth; i++)
        {
            int rowNum = headerOffset + 1 + i; 
            
            var row = new AttendanceTimesheetRow
            {
                Date = new DateOnly(year, month, i + 1),
                ClockIn = ParseTime(sheet.Cell($"B{rowNum}")),
                ClockOut = ParseTime(sheet.Cell($"C{rowNum}")),
                BreakStart = ParseTime(sheet.Cell($"D{rowNum}")),
                BreakEnd = ParseTime(sheet.Cell($"E{rowNum}")),
                OtherInterruption = ParseString(sheet.Cell($"F{rowNum}").GetString()),
                HoursWithoutBreak = (decimal?)ParseDouble(sheet.Cell($"G{rowNum}")), 
                HoursObligation = (decimal?)ParseDouble(sheet.Cell($"H{rowNum}"))
            };
            
            rows.Add(row);
        }

        return new AttendanceTimesheet
        {
            EmployeeCode = employeeCode,
            EmployeeName = employeeName,
            Year = year,
            Month = month,
            Rows = rows
        };
    }


    private static TimeOnly? ParseTime(IXLCell cell)
    {
        if (cell.IsEmpty())
            return null;

        try
        {
            // Excel často ukládá časy jako DateTime
            if (cell.TryGetValue(out DateTime dateTime))
            {
                return TimeOnly.FromDateTime(dateTime);
            }

            // Nebo jako číslo (fraction of day)
            if (cell.TryGetValue(out double timeValue))
            {
                if (timeValue >= 0 && timeValue < 1)
                {
                    // Přesnější výpočet času z double
                    return TimeOnly.FromTimeSpan(TimeSpan.FromDays(timeValue));
                }
            }

            // Pokus o parsování textové hodnoty
            string text = cell.GetString().Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (TimeOnly.TryParseExact(text, new[] { "H:mm", "HH:mm", "H.mm", "HH.mm" }, 
                    CzechCulture, DateTimeStyles.None, out TimeOnly time))
                {
                    return time;
                }
            }
        }
        catch
        {
            // Ignore parsing errors 
            // TODO: přidat logování eroru
        }

        return null;
    }

    private static string? ParseString(string value)
    {
        return value?.Trim();
    }

    private static double? ParseDouble(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        try
        {
            if (cell.TryGetValue(out double numValue))
            {
                return numValue;
            }

            string text = cell.GetString().Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                // Nahradit čárku za tečku pro parsování
                text = text.Replace(',', '.');
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
                {
                    return parsed;
                }
            }
        }
        catch
        {
            // Ignore parsing errors
             // TODO: přidat logování eroru
        }

        return null;
    }
}
 

public sealed class ProjectTimesheetReader : ITimesheetReader<ProjectTimesheet>
{
    public ProjectTimesheet Read(Stream stream)
    {
        throw new NotImplementedException();
    }
}