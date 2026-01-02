namespace Timesheets.Api.Projects;

public sealed record ProjectContractItem(Guid Id, string Name, string RegistrationNumber, DateTime StartDate, DateTime? EndDate, int EmployeeCount);
