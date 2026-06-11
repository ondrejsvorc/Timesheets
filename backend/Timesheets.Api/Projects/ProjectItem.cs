namespace Timesheets.Api.Projects;

public sealed record ProjectItem(Guid Id, string Name, string RegistrationNumber, DateTime StartDate, DateTime? EndDate, DateTime? ArchivedAt, int ContractCount);
