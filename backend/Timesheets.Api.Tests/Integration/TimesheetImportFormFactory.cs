using System.Net.Http.Headers;

namespace Timesheets.Api.Tests.Integration;

internal static class TimesheetImportFormFactory
{
    public static MultipartFormDataContent Create(Guid employeeId, byte[] fileBytes, string fileName)
    {
        MultipartFormDataContent content = new();
        content.Add(new StringContent(employeeId.ToString()), "EmployeeId");

        ByteArrayContent fileContent = new(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "File", fileName);

        return content;
    }
}
