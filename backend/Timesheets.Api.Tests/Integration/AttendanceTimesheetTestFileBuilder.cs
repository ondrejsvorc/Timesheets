using ClosedXML.Excel;

namespace Timesheets.Api.Tests.Integration;

internal static class AttendanceTimesheetTestFileBuilder
{
    public static byte[] Create(string personalNumber, string employeeName, int year, int month, decimal workloadPercent = 50m)
    {
        using XLWorkbook workbook = new();
        IXLWorksheet sheet = workbook.Worksheets.Add("Docházka");

        sheet.Cell("A1").Value = $"{personalNumber} {employeeName}";
        int daysInMonth = DateTime.DaysInMonth(year, month);
        sheet.Cell("A2").Value = $"01.{month:00}.{year} - {daysInMonth}.{month:00}.{year}  {workloadPercent:0}%";

        const int headerOffset = 3;
        for (int day = 1; day <= daysInMonth; day++)
        {
            int rowNum = headerOffset + day;
            if (day is 1 or 2)
            {
                sheet.Cell($"B{rowNum}").Value = "07:30";
                sheet.Cell($"C{rowNum}").Value = "15:30";
                sheet.Cell($"D{rowNum}").Value = "11:30";
                sheet.Cell($"E{rowNum}").Value = "12:00";
            }
        }

        using MemoryStream stream = new();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
