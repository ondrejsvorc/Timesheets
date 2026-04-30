namespace Timesheets.Api.Common;

public static class EmployeeNameFormatter
{
    public static string Format(string? titleBefore, string fullName, string? titleAfter)
    {
        string before = string.IsNullOrWhiteSpace(titleBefore) ? string.Empty : titleBefore.Trim() + " ";
        string after = string.IsNullOrWhiteSpace(titleAfter) ? string.Empty : " " + titleAfter.Trim();
        return before + fullName + after;
    }
}

