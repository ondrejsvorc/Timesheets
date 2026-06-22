namespace Timesheets.Api.Data;

public static class ContractSchema
{
    public static class Name
    {
        public const int MaxLength = 200;
    }

    public static class RegistrationNumber
    {
        public const int MaxLength = 100;
        public const string Pattern = @"^\d{5} \d{2} \d{4} \d{2}$";
    }
}
