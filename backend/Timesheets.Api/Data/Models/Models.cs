namespace Timesheets.Api.Data.Models;

public sealed class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ProjectManager> ProjectManagers { get; set; } = [];
    public ICollection<Contract> Contracts { get; set; } = [];
}

public sealed class ProjectManager
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid EmployeeId { get; set; }

    public Project Project { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}

public sealed class Contract
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Project Project { get; set; } = null!;
    public ICollection<ProjectTimesheet> ProjectTimesheets { get; set; } = [];
    public ICollection<ContractManager> ContractManagers { get; set; } = [];
    public ICollection<ContractEmployee> ContractEmployees { get; set; } = [];
}

public sealed class ContractManager
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public Guid EmployeeId { get; set; }

    public Contract Contract { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}

public sealed class ContractEmployee
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public Guid EmployeeId { get; set; }
    public string PositionCode { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal Workload { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public Contract Contract { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}

public sealed class Employee
{
    public Guid Id { get; set; }
    public Guid? EmployeeTypeId { get; set; }
    public int PersonalNumber { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsGlobalManager { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public EmployeeType EmployeeType { get; set; } = null!;
    public ICollection<AttendanceTimesheet> AttendanceTimesheets { get; set; } = [];
    public ICollection<ProjectTimesheet> ProjectTimesheets { get; set; } = [];
    public ICollection<CoreEmployment> CoreEmployments { get; set; } = [];
    public ICollection<EmployeeWorkload> EmployeeWorkloads { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
}

public sealed class EmployeeType
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Employee> Employees { get; set; } = [];
}

public sealed class CoreEmployment
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public decimal Workload { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public Employee Employee { get; set; } = null!;
}

public sealed class EmployeeWorkload
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Workload { get; set; }

    public Employee Employee { get; set; } = null!;
}

public sealed class Notification
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;

    public Employee Employee { get; set; } = null!;
}

public sealed class AttendanceTimesheet
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid TimesheetStatusId { get; set; }
    public Guid? ApprovedBy { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Employee Employee { get; set; } = null!;
    public TimesheetStatus TimesheetStatus { get; set; } = null!;
    public Employee ApprovedByEmployee { get; set; } = null!;
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
    public decimal Workload { get; set; }
    public decimal HoursWithoutBreak { get; set; }
    public decimal HoursObligation { get; set; }
    public bool IsHoliday { get; set; }
    public string? Description { get; set; }
    public string Schedules { get; set; } = "[]";

    public AttendanceTimesheet AttendanceTimesheet { get; set; } = null!;
    public ICollection<DayInterruption> DayInterruptions { get; set; } = [];
}

public sealed class DayInterruption
{
    public Guid Id { get; set; }
    public Guid AttendanceDayId { get; set; }
    public Guid InterruptionId { get; set; }

    public AttendanceDay AttendanceDay { get; set; } = null!;
    public Interruption Interruption { get; set; } = null!;
}

public sealed class Interruption
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? HoursObligationOverride { get; set; }
}

public sealed class ProjectTimesheet
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid ContractId { get; set; }
    public Guid ContractEmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Workload { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
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

    public ProjectTimesheet ProjectTimesheet { get; set; } = null!;
}

public sealed class TimesheetStatus
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<AttendanceTimesheet> AttendanceTimesheets { get; set; } = [];
}