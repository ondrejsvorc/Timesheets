namespace Timesheets.Api.Data.Models;

public sealed class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? RegistrationNumber { get; set; }
    public string? RecipientName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ProjectManager> Managers { get; set; } = [];
    public ICollection<Contract> Contracts { get; set; } = [];
}

public sealed class ProjectManager
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid EmployeeId { get; set; }
}

public sealed class Contract
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Project Project { get; set; } = null!;
    public ICollection<ContractManager> Managers { get; set; } = [];
    public ICollection<ContractEmployee> Employees { get; set; } = [];
    public ICollection<AttendanceTimesheet> AttendanceTimesheets { get; set; } = [];
    public ICollection<ProjectTimesheet> ProjectTimesheets { get; set; } = [];
}

public sealed class ContractManager
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public Guid EmployeeId { get; set; }
}

public sealed class ContractEmployee
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public Guid EmployeeId { get; set; }

    public string? Position { get; set; }
    public decimal? Workload { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public sealed class Employee
{
    public Guid Id { get; set; }
    public Guid EmployeeTypeId { get; set; }
    public int PersonalNumber { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsGlobalManager { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public EmployeeType EmployeeType { get; set; } = null!;
}

public sealed class EmployeeType
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class AttendanceTimesheet
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid ContractId { get; set; }
    public Guid TimesheetStatusId { get; set; }
    public Guid? ApprovedBy { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<AttendanceDay> Days { get; set; } = [];
}

public sealed class AttendanceDay
{
    public Guid Id { get; set; }
    public Guid AttendanceTimesheetId { get; set; }

    public DateTime Date { get; set; }
    public TimeSpan? ClockIn { get; set; }
    public TimeSpan? ClockOut { get; set; }
    public TimeSpan? BreakStart { get; set; }
    public TimeSpan? BreakEnd { get; set; }

    public Guid? InterruptionId { get; set; }

    public decimal HoursWithoutBreak { get; set; }
    public decimal HoursObligation { get; set; }

    public bool IsHoliday { get; set; }
    public string? Description { get; set; }
}

public sealed class Interruption
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public decimal? HoursObligationOverride { get; set; }
}

public sealed class ProjectTimesheet
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid ContractId { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }

    public decimal Workload { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ProjectDay> Days { get; set; } = [];
}

public sealed class ProjectDay
{
    public Guid Id { get; set; }
    public Guid ProjectTimesheetId { get; set; }

    public DateTime Date { get; set; }
    public decimal Hours { get; set; }
    public bool IsHoliday { get; set; }
    public decimal Workload { get; set; }
    public decimal HoursObligation { get; set; }
}

public sealed class TimesheetStatus
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class Notification
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}
