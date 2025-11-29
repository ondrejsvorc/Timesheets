using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<ProjectManager> ProjectManagers { get; set; } = null!;
    public DbSet<Contract> Contracts { get; set; } = null!;
    public DbSet<ContractManager> ContractManagers { get; set; } = null!;
    public DbSet<ContractEmployee> ContractEmployees { get; set; } = null!;

    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<EmployeeType> EmployeeTypes { get; set; } = null!;

    public DbSet<AttendanceTimesheet> AttendanceTimesheets { get; set; } = null!;
    public DbSet<AttendanceDay> AttendanceDays { get; set; } = null!;
    public DbSet<Interruption> Interruptions { get; set; } = null!;

    public DbSet<ProjectTimesheet> ProjectTimesheets { get; set; } = null!;
    public DbSet<ProjectDay> ProjectDays { get; set; } = null!;

    public DbSet<TimesheetStatus> TimesheetStatuses { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // konfigurace přijde později
    }
}
