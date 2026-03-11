using ClosedXML.Excel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Timesheets.Api.Timesheets;

public sealed record AttendanceTimesheetMetadata(
    int EmployeePersonalNumber,
    string? EmployeeName,
    decimal Workload,
    int Year,
    int Month,
    int DaysInMonth
);

public interface IAttendanceTimesheetMetadataReader
{
    AttendanceTimesheetMetadata Read(Stream stream);
}

public sealed class AttendanceTimesheetMetadataReader : IAttendanceTimesheetMetadataReader
{
    public AttendanceTimesheetMetadata Read(Stream stream)
    {
        using XLWorkbook workbook = new(stream);
        IXLWorksheet sheet = workbook.Worksheets.Worksheet(1);
        return AttendanceTimesheetWorksheetParser.ReadMetadata(sheet);
    }
}

internal static partial class AttendanceTimesheetWorksheetParser
{
    [GeneratedRegex(@"^(\d+)\s+(.+)$")]
    private static partial Regex EmployeeRegex();

    [GeneratedRegex(@"(\d{2})\.(\d{2})\.(\d{4})")]
    private static partial Regex PeriodRegex();

    [GeneratedRegex(@"(\d+)\s*%")]
    private static partial Regex WorkloadRegex();

    public static AttendanceTimesheetMetadata ReadMetadata(IXLWorksheet sheet)
    {
        string cellA1 = sheet.Cell("A1").GetString();
        Match employeeMatch = EmployeeRegex().Match(cellA1);

        int employeePersonalNumber = 0;
        string? employeeName = null;

        if (employeeMatch.Success)
        {
            employeePersonalNumber = int.Parse(employeeMatch.Groups[1].Value);
            employeeName = employeeMatch.Groups[2].Value.Trim();
        }

        string cellA2 = sheet.Cell("A2").GetString();
        Match periodMatch = PeriodRegex().Match(cellA2);
        Match workloadMatch = WorkloadRegex().Match(cellA2);

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

        return new AttendanceTimesheetMetadata(
            EmployeePersonalNumber: employeePersonalNumber,
            EmployeeName: employeeName,
            Workload: workload,
            Year: year,
            Month: month,
            DaysInMonth: daysInMonth
        );
    }
}
