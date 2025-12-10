using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Data;

public static class DatabaseSeeder
{
    public static async Task SeedTestDataAsync(AppDbContext context)
    {
        if (!context.Employees.Any())
        {
            List<Employee> employees =
            [
                new()
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    EmployeeTypeId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    PersonalNumber = 1001,
                    FullName = "Jan Novák",
                    Email = "jan.novak@example.com",
                    IsGlobalManager = true
                },
                new()
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                    EmployeeTypeId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    PersonalNumber = 1002,
                    FullName = "Marie Svobodová",
                    Email = "marie.svobodova@example.com",
                    IsGlobalManager = false
                },
                new()
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                    EmployeeTypeId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                    PersonalNumber = 2001,
                    FullName = "Petr Dvořák",
                    Email = "petr.dvorak@example.com",
                    IsGlobalManager = false
                }
            ];
            context.Employees.AddRange(employees);
        }

        if (!context.Projects.Any())
        {
            List<Project> projects =
            [
                new()
                {
                    Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                    Name = "Výzkumný projekt Alpha",
                    RegistrationNumber = "PROJ-2024-001",
                    RecipientName = "Univerzita XYZ",
                    StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                    Description = "Hlavní výzkumný projekt"
                },
                new()
                {
                    Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                    Name = "Vývojový projekt Beta",
                    RegistrationNumber = "PROJ-2024-002",
                    RecipientName = "Společnost ABC",
                    StartDate = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                    Description = "Vývoj nového systému"
                }
            ];
            context.Projects.AddRange(projects);
        }

        if (!context.Contracts.Any())
        {
            List<Contract> contracts =
            [
                new()
                {
                    Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                    ProjectId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                    Name = "Kontrakt Alpha-1",
                    RegistrationNumber = "CONT-2024-001",
                    StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                    Description = "Hlavní kontrakt projektu Alpha"
                },
                new()
                {
                    Id = Guid.Parse("30000000-0000-0000-0000-000000000002"),
                    ProjectId = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                    Name = "Kontrakt Beta-1",
                    RegistrationNumber = "CONT-2024-002",
                    StartDate = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                    Description = "Hlavní kontrakt projektu Beta"
                }
            ];
            context.Contracts.AddRange(contracts);
        }

        if (!context.ProjectManagers.Any())
        {
            List<ProjectManager> projectManagers =
            [
                new()
                {
                    Id = Guid.Parse("40000000-0000-0000-0000-000000000001"),
                    ProjectId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                    EmployeeId = Guid.Parse("10000000-0000-0000-0000-000000000001")
                },
                new()
                {
                    Id = Guid.Parse("40000000-0000-0000-0000-000000000002"),
                    ProjectId = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                    EmployeeId = Guid.Parse("10000000-0000-0000-0000-000000000001")
                }
            ];
            context.ProjectManagers.AddRange(projectManagers);
        }

        if (!context.ContractManagers.Any())
        {
            List<ContractManager> contractManagers =
            [
                new()
                {
                    Id = Guid.Parse("50000000-0000-0000-0000-000000000001"),
                    ContractId = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                    EmployeeId = Guid.Parse("10000000-0000-0000-0000-000000000001")
                },
                new()
                {
                    Id = Guid.Parse("50000000-0000-0000-0000-000000000002"),
                    ContractId = Guid.Parse("30000000-0000-0000-0000-000000000002"),
                    EmployeeId = Guid.Parse("10000000-0000-0000-0000-000000000001")
                }
            ];
            context.ContractManagers.AddRange(contractManagers);
        }

        if (!context.ContractEmployees.Any())
        {
            List<ContractEmployee> contractEmployees =
            [
                // Marie Svobodová – dvě pozice
                new()
                {
                    Id = Guid.Parse("60000000-0000-0000-0000-000000000001"),
                    ContractId = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                    EmployeeId = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                    Position = "Výzkumný pracovník",
                    Workload = 1.0m,
                    StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new()
                {
                    Id = Guid.Parse("60000000-0000-0000-0000-000000000003"),
                    ContractId = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                    EmployeeId = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                    Position = "Koordinátor",
                    Workload = 0.25m,
                    StartDate = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc)
                },

                // Petr Dvořák – jedna pozice
                new()
                {
                    Id = Guid.Parse("60000000-0000-0000-0000-000000000002"),
                    ContractId = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                    EmployeeId = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                    Position = "Technik",
                    Workload = 0.5m,
                    StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            ];
            context.ContractEmployees.AddRange(contractEmployees);
        }

        if (!context.AttendanceTimesheets.Any())
        {
            List<AttendanceTimesheet> attendanceTimesheets =
            [
                new()
                {
                    Id = Guid.Parse("70000000-0000-0000-0000-000000000001"),
                    EmployeeId = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                    ContractId = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                    TimesheetStatusId = Guid.Parse("00000000-0000-0000-0000-000000000022"), // Schválený
                    Year = 2024,
                    Month = 11,
                    SubmittedAt = new DateTime(2024, 11, 30, 10, 0, 0, DateTimeKind.Utc),
                    ApprovedAt = new DateTime(2024, 12, 1, 14, 30, 0, DateTimeKind.Utc),
                    ApprovedBy = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    CreatedAt = new DateTime(2024, 11, 1, 8, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 12, 1, 14, 30, 0, DateTimeKind.Utc)
                },
                new()
                {
                    Id = Guid.Parse("70000000-0000-0000-0000-000000000002"),
                    EmployeeId = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                    ContractId = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                    TimesheetStatusId = Guid.Parse("00000000-0000-0000-0000-000000000021"), // Ke schválení
                    Year = 2024,
                    Month = 12,
                    SubmittedAt = new DateTime(2024, 12, 1, 9, 15, 0, DateTimeKind.Utc),
                    CreatedAt = new DateTime(2024, 12, 1, 8, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 12, 1, 9, 15, 0, DateTimeKind.Utc)
                },
                new()
                {
                    Id = Guid.Parse("70000000-0000-0000-0000-000000000003"),
                    EmployeeId = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                    ContractId = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                    TimesheetStatusId = Guid.Parse("00000000-0000-0000-0000-000000000020"), // Rozpracovaný
                    Year = 2024,
                    Month = 12,
                    CreatedAt = new DateTime(2024, 12, 1, 8, 0, 0, DateTimeKind.Utc)
                }
            ];
            context.AttendanceTimesheets.AddRange(attendanceTimesheets);
        }

        if (!context.AttendanceDays.Any())
        {
            List<AttendanceDay> attendanceDays =
            [
                // Listopad 2024 - Marie Svobodová (schválený timesheet)
                new()
                {
                    Id = Guid.Parse("80000000-0000-0000-0000-000000000001"),
                    AttendanceTimesheetId = Guid.Parse("70000000-0000-0000-0000-000000000001"),
                    Date = new DateTime(2024, 11, 1, 0, 0, 0, DateTimeKind.Utc),
                    ClockIn = new TimeSpan(8, 0, 0),
                    ClockOut = new TimeSpan(16, 30, 0),
                    BreakStart = new TimeSpan(12, 0, 0),
                    BreakEnd = new TimeSpan(12, 30, 0),
                    HoursWithoutBreak = 8.5m,
                    HoursObligation = 8.0m,
                    IsHoliday = false,
                    Workload = 1.0m
                },
                new()
                {
                    Id = Guid.Parse("80000000-0000-0000-0000-000000000002"),
                    AttendanceTimesheetId = Guid.Parse("70000000-0000-0000-0000-000000000001"),
                    Date = new DateTime(2024, 11, 2, 0, 0, 0, DateTimeKind.Utc),
                    ClockIn = new TimeSpan(8, 15, 0),
                    ClockOut = new TimeSpan(16, 45, 0),
                    BreakStart = new TimeSpan(12, 0, 0),
                    BreakEnd = new TimeSpan(12, 30, 0),
                    HoursWithoutBreak = 8.5m,
                    HoursObligation = 8.0m,
                    IsHoliday = false,
                    Workload = 1.0m
                },
                new()
                {
                    Id = Guid.Parse("80000000-0000-0000-0000-000000000003"),
                    AttendanceTimesheetId = Guid.Parse("70000000-0000-0000-0000-000000000001"),
                    Date = new DateTime(2024, 11, 3, 0, 0, 0, DateTimeKind.Utc),
                    HoursObligation = 0m,
                    IsHoliday = false,
                    Workload = 1.0m
                },
                new()
                {
                    Id = Guid.Parse("80000000-0000-0000-0000-000000000004"),
                    AttendanceTimesheetId = Guid.Parse("70000000-0000-0000-0000-000000000001"),
                    Date = new DateTime(2024, 11, 4, 0, 0, 0, DateTimeKind.Utc),
                    ClockIn = new TimeSpan(8, 0, 0),
                    ClockOut = new TimeSpan(16, 30, 0),
                    BreakStart = new TimeSpan(12, 0, 0),
                    BreakEnd = new TimeSpan(12, 30, 0),
                    HoursWithoutBreak = 8.5m,
                    HoursObligation = 8.0m,
                    IsHoliday = false,
                    Workload = 1.0m
                },
                new()
                {
                    Id = Guid.Parse("80000000-0000-0000-0000-000000000005"),
                    AttendanceTimesheetId = Guid.Parse("70000000-0000-0000-0000-000000000001"),
                    Date = new DateTime(2024, 11, 5, 0, 0, 0, DateTimeKind.Utc),
                    ClockIn = new TimeSpan(8, 0, 0),
                    ClockOut = new TimeSpan(10, 30, 0),
                    HoursWithoutBreak = 2.5m,
                    HoursObligation = 2.5m,
                    IsHoliday = false,
                    Workload = 1.0m
                },
                // Prosinec 2024 - Marie Svobodová (ke schválení)
                new()
                {
                    Id = Guid.Parse("80000000-0000-0000-0000-000000000006"),
                    AttendanceTimesheetId = Guid.Parse("70000000-0000-0000-0000-000000000002"),
                    Date = new DateTime(2024, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                    ClockIn = new TimeSpan(8, 0, 0),
                    ClockOut = new TimeSpan(16, 30, 0),
                    BreakStart = new TimeSpan(12, 0, 0),
                    BreakEnd = new TimeSpan(12, 30, 0),
                    HoursWithoutBreak = 8.5m,
                    HoursObligation = 8.0m,
                    IsHoliday = false,
                    Workload = 1.0m
                },
                new()
                {
                    Id = Guid.Parse("80000000-0000-0000-0000-000000000007"),
                    AttendanceTimesheetId = Guid.Parse("70000000-0000-0000-0000-000000000002"),
                    Date = new DateTime(2024, 12, 2, 0, 0, 0, DateTimeKind.Utc),
                    ClockIn = new TimeSpan(8, 0, 0),
                    ClockOut = new TimeSpan(16, 30, 0),
                    BreakStart = new TimeSpan(12, 0, 0),
                    BreakEnd = new TimeSpan(12, 30, 0),
                    HoursWithoutBreak = 8.5m,
                    HoursObligation = 8.0m,
                    IsHoliday = false,
                    Workload = 1.0m
                },
                // Prosinec 2024 - Petr Dvořák (rozpracovaný)
                new()
                {
                    Id = Guid.Parse("80000000-0000-0000-0000-000000000008"),
                    AttendanceTimesheetId = Guid.Parse("70000000-0000-0000-0000-000000000003"),
                    Date = new DateTime(2024, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                    ClockIn = new TimeSpan(7, 30, 0),
                    ClockOut = new TimeSpan(15, 30, 0),
                    BreakStart = new TimeSpan(11, 30, 0),
                    BreakEnd = new TimeSpan(12, 0, 0),
                    HoursWithoutBreak = 8.0m,
                    HoursObligation = 8.0m,
                    IsHoliday = false,
                    Workload = 0.5m
                },
                new()
                {
                    Id = Guid.Parse("80000000-0000-0000-0000-000000000009"),
                    AttendanceTimesheetId = Guid.Parse("70000000-0000-0000-0000-000000000003"),
                    Date = new DateTime(2024, 12, 2, 0, 0, 0, DateTimeKind.Utc),
                    ClockIn = new TimeSpan(7, 30, 0),
                    ClockOut = new TimeSpan(15, 30, 0),
                    BreakStart = new TimeSpan(11, 30, 0),
                    BreakEnd = new TimeSpan(12, 0, 0),
                    HoursWithoutBreak = 8.0m,
                    HoursObligation = 8.0m,
                    IsHoliday = false,
                    Workload = 0.5m
                }
            ];
            context.AttendanceDays.AddRange(attendanceDays);
        }

        if (!context.ProjectTimesheets.Any())
        {
            List<ProjectTimesheet> projectTimesheets =
            [
                new()
                {
                    Id = Guid.Parse("90000000-0000-0000-0000-000000000001"),
                    EmployeeId = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                    ContractId = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                    Year = 2024,
                    Month = 11,
                    Workload = 1.0m,
                    CreatedAt = new DateTime(2024, 11, 1, 8, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 11, 30, 18, 0, 0, DateTimeKind.Utc)
                },
                new()
                {
                    Id = Guid.Parse("90000000-0000-0000-0000-000000000002"),
                    EmployeeId = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                    ContractId = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                    Year = 2024,
                    Month = 12,
                    Workload = 1.0m,
                    CreatedAt = new DateTime(2024, 12, 1, 8, 0, 0, DateTimeKind.Utc)
                },
                new()
                {
                    Id = Guid.Parse("90000000-0000-0000-0000-000000000003"),
                    EmployeeId = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                    ContractId = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                    Year = 2024,
                    Month = 12,
                    Workload = 0.5m,
                    CreatedAt = new DateTime(2024, 12, 1, 8, 0, 0, DateTimeKind.Utc)
                }
            ];
            context.ProjectTimesheets.AddRange(projectTimesheets);
        }

        if (!context.ProjectDays.Any())
        {
            List<ProjectDay> projectDays =
            [
                // Listopad 2024 - Marie Svobodová
                new()
                {
                    Id = Guid.Parse("A0000000-0000-0000-0000-000000000001"),
                    ProjectTimesheetId = Guid.Parse("90000000-0000-0000-0000-000000000001"),
                    Date = new DateTime(2024, 11, 1, 0, 0, 0, DateTimeKind.Utc),
                    Hours = 8.0m,
                    Workload = 1.0m,
                    HoursObligation = 8.0m,
                    IsHoliday = false
                },
                new()
                {
                    Id = Guid.Parse("A0000000-0000-0000-0000-000000000002"),
                    ProjectTimesheetId = Guid.Parse("90000000-0000-0000-0000-000000000001"),
                    Date = new DateTime(2024, 11, 2, 0, 0, 0, DateTimeKind.Utc),
                    Hours = 8.0m,
                    Workload = 1.0m,
                    HoursObligation = 8.0m,
                    IsHoliday = false
                },
                new()
                {
                    Id = Guid.Parse("A0000000-0000-0000-0000-000000000003"),
                    ProjectTimesheetId = Guid.Parse("90000000-0000-0000-0000-000000000001"),
                    Date = new DateTime(2024, 11, 3, 0, 0, 0, DateTimeKind.Utc),
                    Hours = 0m,
                    Workload = 1.0m,
                    HoursObligation = 0m,
                    IsHoliday = false
                },
                new()
                {
                    Id = Guid.Parse("A0000000-0000-0000-0000-000000000004"),
                    ProjectTimesheetId = Guid.Parse("90000000-0000-0000-0000-000000000001"),
                    Date = new DateTime(2024, 11, 4, 0, 0, 0, DateTimeKind.Utc),
                    Hours = 8.0m,
                    Workload = 1.0m,
                    HoursObligation = 8.0m,
                    IsHoliday = false
                },
                new()
                {
                    Id = Guid.Parse("A0000000-0000-0000-0000-000000000005"),
                    ProjectTimesheetId = Guid.Parse("90000000-0000-0000-0000-000000000001"),
                    Date = new DateTime(2024, 11, 5, 0, 0, 0, DateTimeKind.Utc),
                    Hours = 2.5m,
                    Workload = 1.0m,
                    HoursObligation = 2.5m,
                    IsHoliday = false
                },
                // Prosinec 2024 - Marie Svobodová
                new()
                {
                    Id = Guid.Parse("A0000000-0000-0000-0000-000000000006"),
                    ProjectTimesheetId = Guid.Parse("90000000-0000-0000-0000-000000000002"),
                    Date = new DateTime(2024, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                    Hours = 8.0m,
                    Workload = 1.0m,
                    HoursObligation = 8.0m,
                    IsHoliday = false
                },
                new()
                {
                    Id = Guid.Parse("A0000000-0000-0000-0000-000000000007"),
                    ProjectTimesheetId = Guid.Parse("90000000-0000-0000-0000-000000000002"),
                    Date = new DateTime(2024, 12, 2, 0, 0, 0, DateTimeKind.Utc),
                    Hours = 8.0m,
                    Workload = 1.0m,
                    HoursObligation = 8.0m,
                    IsHoliday = false
                },
                // Prosinec 2024 - Petr Dvořák
                new()
                {
                    Id = Guid.Parse("A0000000-0000-0000-0000-000000000008"),
                    ProjectTimesheetId = Guid.Parse("90000000-0000-0000-0000-000000000003"),
                    Date = new DateTime(2024, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                    Hours = 4.0m,
                    Workload = 0.5m,
                    HoursObligation = 4.0m,
                    IsHoliday = false
                },
                new()
                {
                    Id = Guid.Parse("A0000000-0000-0000-0000-000000000009"),
                    ProjectTimesheetId = Guid.Parse("90000000-0000-0000-0000-000000000003"),
                    Date = new DateTime(2024, 12, 2, 0, 0, 0, DateTimeKind.Utc),
                    Hours = 4.0m,
                    Workload = 0.5m,
                    HoursObligation = 4.0m,
                    IsHoliday = false
                }
            ];
            context.ProjectDays.AddRange(projectDays);
        }

        if (!context.Notifications.Any())
        {
            List<Notification> notifications =
            [
                new()
                {
                    Id = Guid.Parse("B0000000-0000-0000-0000-000000000001"),
                    EmployeeId = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                    Message = "Váš timesheet za listopad 2024 byl schválen.",
                    CreatedAt = new DateTime(2024, 12, 1, 14, 30, 0, DateTimeKind.Utc),
                    IsRead = true
                },
                new()
                {
                    Id = Guid.Parse("B0000000-0000-0000-0000-000000000002"),
                    EmployeeId = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                    Message = "Váš timesheet za prosinec 2024 čeká na schválení.",
                    CreatedAt = new DateTime(2024, 12, 1, 9, 20, 0, DateTimeKind.Utc),
                    IsRead = false
                },
                new()
                {
                    Id = Guid.Parse("B0000000-0000-0000-0000-000000000003"),
                    EmployeeId = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                    Message = "Nezapomeňte vyplnit timesheet za prosinec 2024.",
                    CreatedAt = new DateTime(2024, 12, 1, 8, 0, 0, DateTimeKind.Utc),
                    IsRead = false
                },
                new()
                {
                    Id = Guid.Parse("B0000000-0000-0000-0000-000000000004"),
                    EmployeeId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    Message = "Máte nové timesheety ke schválení.",
                    CreatedAt = new DateTime(2024, 12, 1, 9, 25, 0, DateTimeKind.Utc),
                    IsRead = false
                }
            ];
            context.Notifications.AddRange(notifications);
        }

        if (!context.DayInterruptions.Any())
        {
            List<DayInterruption> dayInterruptions =
            [
                // Dovolená na dni 2024-11-03 (Marie, schválený timesheet)
                new()
                {
                    Id = Guid.Parse("C0000000-0000-0000-0000-000000000001"),
                    AttendanceDayId = Guid.Parse("80000000-0000-0000-0000-000000000003"),
                    InterruptionId = Guid.Parse("00000000-0000-0000-0000-000000000010") // D
                },
                // Krátká návštěva lékaře na dni 2024-11-05 (Marie, schválený timesheet)
                new()
                {
                    Id = Guid.Parse("C0000000-0000-0000-0000-000000000002"),
                    AttendanceDayId = Guid.Parse("80000000-0000-0000-0000-000000000005"),
                    InterruptionId = Guid.Parse("00000000-0000-0000-0000-000000000017") // NK
                },
                // Dovolená na dni 2024-12-01 (Petr, rozpracovaný timesheet)
                new()
                {
                    Id = Guid.Parse("C0000000-0000-0000-0000-000000000003"),
                    AttendanceDayId = Guid.Parse("80000000-0000-0000-0000-000000000008"),
                    InterruptionId = Guid.Parse("00000000-0000-0000-0000-000000000010") // D
                }
            ];
            context.DayInterruptions.AddRange(dayInterruptions);
        }

        await context.SaveChangesAsync();
    }
}

