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
    public DbSet<DayInterruption> DayInterruptions { get; set; } = null!;
    public DbSet<Interruption> Interruptions { get; set; } = null!;

    public DbSet<ProjectTimesheet> ProjectTimesheets { get; set; } = null!;
    public DbSet<ProjectDay> ProjectDays { get; set; } = null!;
    public DbSet<CoreEmployment> CoreEmployments { get; set; } = null!;
    public DbSet<EmployeeWorkload> EmployeeWorkloads { get; set; } = null!;

    public DbSet<TimesheetStatus> TimesheetStatuses { get; set; } = null!;
    public DbSet<TimesheetStatusHistory> TimesheetStatusHistories { get; set; } = null!;
    public DbSet<TimesheetComment> TimesheetComments { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureEmployeesTable(modelBuilder);
        ConfigureEmployeeTypesTable(modelBuilder);

        ConfigureProjectsTable(modelBuilder);
        ConfigureProjectManagersTable(modelBuilder);

        ConfigureContractsTable(modelBuilder);
        ConfigureContractManagersTable(modelBuilder);
        ConfigureContractEmployeesTable(modelBuilder);

        ConfigureAttendanceTimesheetsTable(modelBuilder);
        ConfigureAttendanceDaysTable(modelBuilder);
        ConfigureInterruptionsTable(modelBuilder);
        ConfigureDayInterruptionsTable(modelBuilder);

        ConfigureProjectTimesheetsTable(modelBuilder);
        ConfigureProjectDaysTable(modelBuilder);
        ConfigureTimesheetStatusesTable(modelBuilder);
        ConfigureTimesheetStatusHistoriesTable(modelBuilder);
        ConfigureTimesheetCommentsTable(modelBuilder);
        ConfigureCoreEmploymentsTable(modelBuilder);
        ConfigureEmployeeWorkloadsTable(modelBuilder);

        ConfigureNotificationsTable(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureEmployeesTable(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<Employee>();

        builder.ToTable("Employee");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FullName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(e => e.Email)
            .IsUnique();

        builder.Property(e => e.PersonalNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.TitleBefore)
            .HasMaxLength(50);

        builder.Property(e => e.TitleAfter)
            .HasMaxLength(50);

        builder.Property(e => e.IsGlobalManager)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne(e => e.EmployeeType)
            .WithMany(et => et.Employees)
            .HasForeignKey(e => e.EmployeeTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.CoreEmployments)
            .WithOne(e => e.Employee)
            .HasForeignKey(ce => ce.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.EmployeeWorkloads)
            .WithOne(e => e.Employee)
            .HasForeignKey(ew => ew.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Notifications)
            .WithOne(e => e.Employee)
            .HasForeignKey(n => n.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureEmployeeTypesTable(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<EmployeeType>();

        builder.ToTable("EmployeeType");

        builder.HasKey(et => et.Id);

        builder.Property(et => et.Name)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasData(
            new EmployeeType { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "Akademik" },
            new EmployeeType { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Name = "Neakademik" }
        );
    }

    private static void ConfigureProjectsTable(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<Project>();

        builder.ToTable("Project");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(ProjectSchema.Name.MaxLength);

        builder.Property(p => p.RegistrationNumber)
            .HasMaxLength(ProjectSchema.RegistrationNumber.MaxLength);

        builder.Property(p => p.StartDate)
            .IsRequired();

        builder.Property(p => p.EndDate);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt);

        builder.HasMany(p => p.ProjectManagers)
            .WithOne(m => m.Project)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Contracts)
            .WithOne(c => c.Project)
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureProjectManagersTable(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<ProjectManager>();

        builder.ToTable("ProjectManager");

        builder.HasKey(pm => pm.Id);

        builder.Property(pm => pm.ProjectId)
            .IsRequired();

        builder.Property(pm => pm.EmployeeId)
            .IsRequired();

        builder.HasIndex(pm => new { pm.ProjectId, pm.EmployeeId })
            .IsUnique();
    }

    private static void ConfigureContractsTable(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<Contract>();

        builder.ToTable("Contract");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.RegistrationNumber)
            .HasMaxLength(100);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt);

        builder.HasMany(c => c.ContractManagers)
            .WithOne(m => m.Contract)
            .HasForeignKey(cm => cm.ContractId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.ContractEmployees)
            .WithOne(e => e.Contract)
            .HasForeignKey(ce => ce.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureContractManagersTable(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<ContractManager>();

        builder.ToTable("ContractManager");

        builder.HasKey(cm => cm.Id);

        builder.Property(cm => cm.ContractId)
            .IsRequired();

        builder.Property(cm => cm.EmployeeId)
            .IsRequired();

        builder.HasIndex(cm => new { cm.ContractId, cm.EmployeeId })
            .IsUnique();
    }

    private static void ConfigureContractEmployeesTable(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<ContractEmployee>();

        builder.ToTable("ContractEmployee");

        builder.HasKey(ce => ce.Id);

        builder.Property(ce => ce.ContractId)
            .IsRequired();

        builder.Property(ce => ce.EmployeeId)
            .IsRequired();

        builder.Property(ce => ce.PositionCode)
            .IsRequired()
            .HasMaxLength(ContractEmployeeSchema.PositionCode.MaxLength);

        builder.Property(ce => ce.Position)
            .IsRequired()
            .HasMaxLength(ContractEmployeeSchema.Position.MaxLength);

        builder.Property(ce => ce.Workload)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(ce => ce.StartDate)
            .IsRequired();

        builder.Property(ce => ce.EndDate);

        builder.HasIndex(ce => new { ce.ContractId, ce.EmployeeId, ce.Position })
            .IsUnique();
    }

    private static void ConfigureAttendanceTimesheetsTable(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<AttendanceTimesheet>();

        builder.ToTable("AttendanceTimesheet");

        builder.HasKey(at => at.Id);

        builder.Property(at => at.EmployeeId).IsRequired();
        builder.Property(at => at.TimesheetStatusId).IsRequired();
        builder.Property(at => at.Year).IsRequired();
        builder.Property(at => at.Month).IsRequired();
        builder.Property(at => at.CreatedAt).IsRequired();

        builder.HasIndex(at => new { at.EmployeeId, at.Year, at.Month })
            .IsUnique();

        builder.HasOne(at => at.Employee)
            .WithMany(e => e.AttendanceTimesheets)
            .HasForeignKey(at => at.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(at => at.TimesheetStatus)
            .WithMany(ts => ts.AttendanceTimesheets)
            .HasForeignKey(at => at.TimesheetStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(at => at.ApprovedByEmployee)
            .WithMany()
            .HasForeignKey(at => at.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(at => at.Days)
            .WithOne(d => d.AttendanceTimesheet)
            .HasForeignKey(ad => ad.AttendanceTimesheetId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureAttendanceDaysTable(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<AttendanceDay>();

        builder.ToTable("AttendanceDay");

        builder.HasKey(ad => ad.Id);

        builder.Property(ad => ad.AttendanceTimesheetId)
            .IsRequired();

        builder.Property(ad => ad.Date)
            .IsRequired();

        builder.Property(ad => ad.ClockIn);

        builder.Property(ad => ad.ClockOut);

        builder.Property(ad => ad.BreakStart);

        builder.Property(ad => ad.BreakEnd);

        builder.Property(ad => ad.IsHoliday)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ad => ad.Workload)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(ad => ad.HoursWithoutBreak)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(ad => ad.HoursObligation)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(ad => ad.CoreHours)
            .IsRequired()
            .HasPrecision(5, 2)
            .HasDefaultValue(0m);

        builder.Property(ad => ad.Schedules)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb");

        builder.HasIndex(ad => new { ad.AttendanceTimesheetId, ad.Date })
            .IsUnique();

        builder.HasMany(ad => ad.DayInterruptions)
            .WithOne(di => di.AttendanceDay)
            .HasForeignKey(di => di.AttendanceDayId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureDayInterruptionsTable(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<DayInterruption>();

        builder.ToTable("DayInterruption");

        builder.HasKey(di => di.Id);

        builder.Property(di => di.AttendanceDayId)
            .IsRequired();

        builder.Property(di => di.InterruptionId)
            .IsRequired();

        builder.HasIndex(di => new { di.AttendanceDayId, di.InterruptionId })
            .IsUnique();

        builder.HasOne<Interruption>(di => di.Interruption)
            .WithMany()
            .HasForeignKey(di => di.InterruptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureInterruptionsTable(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<Interruption>();

        builder.ToTable("Interruption");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.Description)
            .HasMaxLength(200);

        builder.Property(i => i.HoursObligationOverride)
            .HasPrecision(5, 2);

        builder.HasData(
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000010"), Name = "D", Description = "Dovolenka", HoursObligationOverride = 0 },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000011"), Name = "JMV/HO", Description = "práce na dálku od 1.10.2023", HoursObligationOverride = null },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000012"), Name = "KAHO", Description = "Karanténa -home office", HoursObligationOverride = null },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000013"), Name = "M", Description = "Omluvená nepřítomnost - tvůrčí volno", HoursObligationOverride = 0 },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000014"), Name = "MD/OD", Description = "Mateřská dovolená / Otcovská dovolená", HoursObligationOverride = 0 },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000015"), Name = "N", Description = "Nemocenská", HoursObligationOverride = 0 },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000016"), Name = "NA", Description = "Neomluvená absence", HoursObligationOverride = 0 },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000017"), Name = "NK", Description = "Návštěva lékaře - krátkodobá", HoursObligationOverride = null },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000018"), Name = "NL", Description = "Návštěva lékaře - celý den", HoursObligationOverride = 0 },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000019"), Name = "NP", Description = "Pracovní úraz", HoursObligationOverride = 0 },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000020"), Name = "NV", Description = "Náhradní volno za odprac. dobu", HoursObligationOverride = 0 },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000021"), Name = "O", Description = "Ošetřovné", HoursObligationOverride = 0 },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000022"), Name = "OPN", Description = "Osobní překážky", HoursObligationOverride = null },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000023"), Name = "PN", Description = "Narození dítěte", HoursObligationOverride = 0 },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000024"), Name = "PO", Description = "Odběr krve", HoursObligationOverride = null },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000025"), Name = "PS", Description = "Svatba", HoursObligationOverride = 0 },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000026"), Name = "PU", Description = "Úmrtí rod. příslušníka", HoursObligationOverride = 0 },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000027"), Name = "PVB", Description = "Pracovní volno pro brannou povinnost", HoursObligationOverride = 0 },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000028"), Name = "PVM", Description = "Pracovní volno pro s akcí pro děti a mládež", HoursObligationOverride = 0 },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000029"), Name = "PZ", Description = "Překážka na straně zaměstnavatele", HoursObligationOverride = 0 },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000030"), Name = "RD", Description = "Rodičovská dovolená", HoursObligationOverride = 0 },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000031"), Name = "SCP", Description = "Tuzemská služební cesta Projekt", HoursObligationOverride = null },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000032"), Name = "SCS", Description = "Tuzemská služební cesta Stáž", HoursObligationOverride = null },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000033"), Name = "SCT", Description = "Služební cesta", HoursObligationOverride = null },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000034"), Name = "SCZ", Description = "Služební cesta zahraniční", HoursObligationOverride = null },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000035"), Name = "SCZE", Description = "Zahraniční služební cesta Erasmus", HoursObligationOverride = null },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000036"), Name = "SCZP", Description = "Zahraniční služební cesta Projekt", HoursObligationOverride = null },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000037"), Name = "SCZS", Description = "Zahraniční služební cesta Stáž", HoursObligationOverride = null },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000038"), Name = "ST", Description = "Studium s náhradou mzdy", HoursObligationOverride = null },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000039"), Name = "VN", Description = "Neplacené volno", HoursObligationOverride = 0 },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000040"), Name = "VZ", Description = "Nové zaměstnání", HoursObligationOverride = 0 },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000041"), Name = "Z", Description = "Volno pro obecný zájem", HoursObligationOverride = 0 },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000042"), Name = "Zp", Description = "Veřejná funkce - poslanec", HoursObligationOverride = null },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000043"), Name = "Zs", Description = "Dlouhodobý pobyt v cizině", HoursObligationOverride = null },
            new Interruption { Id = Guid.Parse("00000000-0000-0000-0000-000000000044"), Name = "Zv", Description = "Zdravotní volno", HoursObligationOverride = 0 }
        );
    }

    private static void ConfigureProjectTimesheetsTable(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<ProjectTimesheet>();

        builder.ToTable("ProjectTimesheet");

        builder.HasKey(pt => pt.Id);

        builder.Property(pt => pt.EmployeeId)
            .IsRequired();

        builder.Property(pt => pt.ContractId)
            .IsRequired();

        builder.Property(pt => pt.ContractEmployeeId)
            .IsRequired();

        builder.Property(pt => pt.Year)
            .IsRequired();

        builder.Property(pt => pt.Month)
            .IsRequired();

        builder.Property(pt => pt.Workload)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(pt => pt.TimesheetStatusId)
            .IsRequired();

        builder.HasOne(pt => pt.TimesheetStatus)
            .WithMany()
            .HasForeignKey(pt => pt.TimesheetStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(pt => pt.LockedAt);
        builder.Property(pt => pt.LockedBy);

        builder.Property(pt => pt.CreatedAt)
            .IsRequired();

        builder.Property(pt => pt.UpdatedAt);

        builder.HasIndex(pt => new { pt.ContractEmployeeId, pt.Year, pt.Month })
            .IsUnique();

        builder.HasOne<ContractEmployee>()
            .WithMany()
            .HasForeignKey(pt => pt.ContractEmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(pt => pt.Days)
            .WithOne(d => d.ProjectTimesheet)
            .HasForeignKey(pd => pd.ProjectTimesheetId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureProjectDaysTable(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<ProjectDay>();

        builder.ToTable("ProjectDay");

        builder.HasKey(pd => pd.Id);

        builder.Property(pd => pd.ProjectTimesheetId)
            .IsRequired();

        builder.Property(pd => pd.Date)
            .IsRequired();

        builder.Property(pd => pd.Hours)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(pd => pd.IsHoliday)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(pd => pd.Workload)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(pd => pd.HoursObligation)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.HasIndex(pd => new { pd.ProjectTimesheetId, pd.Date })
            .IsUnique();
    }

    private static void ConfigureCoreEmploymentsTable(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<CoreEmployment>();

        builder.ToTable("CoreEmployment");

        builder.HasKey(ce => ce.Id);

        builder.Property(ce => ce.EmployeeId)
            .IsRequired();

        builder.Property(ce => ce.Workload)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(ce => ce.StartDate)
            .IsRequired();

        builder.Property(ce => ce.EndDate);

        builder.HasIndex(ce => new { ce.EmployeeId, ce.StartDate, ce.EndDate });
    }

    private static void ConfigureEmployeeWorkloadsTable(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<EmployeeWorkload>();

        builder.ToTable("EmployeeWorkload");

        builder.HasKey(ew => ew.Id);

        builder.Property(ew => ew.EmployeeId)
            .IsRequired();

        builder.Property(ew => ew.Year)
            .IsRequired();

        builder.Property(ew => ew.Month)
            .IsRequired();

        builder.Property(ew => ew.Workload)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.HasIndex(ew => new { ew.EmployeeId, ew.Year, ew.Month })
            .IsUnique();
    }

    private static void ConfigureTimesheetStatusesTable(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<TimesheetStatus>();

        builder.ToTable("TimesheetStatus");

        builder.HasKey(ts => ts.Id);

        builder.Property(ts => ts.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasData(
            new TimesheetStatus { Id = Guid.Parse("00000000-0000-0000-0000-000000000020"), Name = "Rozpracovaný" },
            new TimesheetStatus { Id = Guid.Parse("00000000-0000-0000-0000-000000000021"), Name = "Ke schválení" },
            new TimesheetStatus { Id = Guid.Parse("00000000-0000-0000-0000-000000000022"), Name = "Schválený" }
        );
    }

    private static void ConfigureTimesheetStatusHistoriesTable(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<TimesheetStatusHistory>();

        builder.ToTable("TimesheetStatusHistory", table =>
        {
            table.HasCheckConstraint(
                "CK_TimesheetStatusHistory_ExactlyOneTimesheet",
                """
                ("AttendanceTimesheetId" IS NOT NULL AND "ProjectTimesheetId" IS NULL)
                OR
                ("AttendanceTimesheetId" IS NULL AND "ProjectTimesheetId" IS NOT NULL)
                """);
        });

        builder.HasKey(history => history.Id);

        builder.Property(history => history.ToStatusId)
            .IsRequired();

        builder.Property(history => history.ChangedByEmployeeId)
            .IsRequired();

        builder.Property(history => history.ChangedAt)
            .IsRequired();

        builder.Property(history => history.Comment)
            .HasMaxLength(500);

        builder.HasOne(history => history.AttendanceTimesheet)
            .WithMany(timesheet => timesheet.StatusHistory)
            .HasForeignKey(history => history.AttendanceTimesheetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(history => history.ProjectTimesheet)
            .WithMany(timesheet => timesheet.StatusHistory)
            .HasForeignKey(history => history.ProjectTimesheetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(history => history.FromStatus)
            .WithMany()
            .HasForeignKey(history => history.FromStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(history => history.ToStatus)
            .WithMany()
            .HasForeignKey(history => history.ToStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(history => history.ChangedByEmployee)
            .WithMany()
            .HasForeignKey(history => history.ChangedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(history => history.AttendanceTimesheetId);
        builder.HasIndex(history => history.ProjectTimesheetId);
        builder.HasIndex(history => history.ChangedByEmployeeId);
        builder.HasIndex(history => history.ChangedAt);
    }

    private static void ConfigureTimesheetCommentsTable(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<TimesheetComment>();

        builder.ToTable("TimesheetComment", table =>
        {
            table.HasCheckConstraint(
                "CK_TimesheetComment_ExactlyOneTimesheet",
                """
                ("AttendanceTimesheetId" IS NOT NULL AND "ProjectTimesheetId" IS NULL)
                OR
                ("AttendanceTimesheetId" IS NULL AND "ProjectTimesheetId" IS NOT NULL)
                """);
        });

        builder.HasKey(comment => comment.Id);

        builder.Property(comment => comment.AuthorEmployeeId)
            .IsRequired();

        builder.Property(comment => comment.Text)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(comment => comment.CreatedAt)
            .IsRequired();

        builder.HasOne(comment => comment.AttendanceTimesheet)
            .WithMany(timesheet => timesheet.Comments)
            .HasForeignKey(comment => comment.AttendanceTimesheetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(comment => comment.ProjectTimesheet)
            .WithMany(timesheet => timesheet.Comments)
            .HasForeignKey(comment => comment.ProjectTimesheetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(comment => comment.AuthorEmployee)
            .WithMany()
            .HasForeignKey(comment => comment.AuthorEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(comment => comment.AttendanceTimesheetId);
        builder.HasIndex(comment => comment.ProjectTimesheetId);
        builder.HasIndex(comment => comment.AuthorEmployeeId);
        builder.HasIndex(comment => comment.CreatedAt);
    }

    private static void ConfigureNotificationsTable(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<Notification>();

        builder.ToTable("Notification");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.EmployeeId)
            .IsRequired();

        builder.Property(n => n.Message)
            .IsRequired();

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        builder.Property(n => n.IsRead)
            .IsRequired()
            .HasDefaultValue(false);
    }
}
