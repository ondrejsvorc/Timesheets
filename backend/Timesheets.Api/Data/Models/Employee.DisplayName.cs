using System.ComponentModel.DataAnnotations.Schema;

namespace Timesheets.Api.Data.Models;

public sealed partial class Employee
{
    [NotMapped]
    public string DisplayName
    {
        get
        {
            string before = string.IsNullOrWhiteSpace(TitleBefore) ? string.Empty : TitleBefore.Trim() + " ";
            string after = string.IsNullOrWhiteSpace(TitleAfter) ? string.Empty : " " + TitleAfter.Trim();
            return before + $"{FirstName} {Surname}".Trim() + after;
        }
    }
}
