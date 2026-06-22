namespace Timesheets.Api.Data;

public static class ContractEmployeeSchema
{
    public static class PositionCode
    {
        public const int MaxLength = 50;
        public const string Pattern = @"^\d\.\d\.\d\.\d\.\d\.\d{2}$";
    }

    public static class Position
    {
        public const int MaxLength = 200;
    }
}
