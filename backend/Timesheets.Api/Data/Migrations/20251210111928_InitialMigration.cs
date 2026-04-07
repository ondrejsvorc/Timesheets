using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Timesheets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeType",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Interruption",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    HoursObligationOverride = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interruption", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Project",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RegistrationNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RecipientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimesheetStatus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimesheetStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employee",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    PersonalNumber = table.Column<int>(type: "integer", nullable: true),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsGlobalManager = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employee_EmployeeType_EmployeeTypeId",
                        column: x => x.EmployeeTypeId,
                        principalTable: "EmployeeType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Contract",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RegistrationNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contract", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contract_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoreEmployment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Workload = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoreEmployment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoreEmployment_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeWorkload",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Workload = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeWorkload", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkload_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notification_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectManager",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectManager", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectManager_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectManager_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceTimesheet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimesheetStatusId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceTimesheet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceTimesheet_Contract_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contract",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceTimesheet_Employee_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceTimesheet_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceTimesheet_TimesheetStatus_TimesheetStatusId",
                        column: x => x.TimesheetStatusId,
                        principalTable: "TimesheetStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContractEmployee",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Workload = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractEmployee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractEmployee_Contract_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contract",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractEmployee_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractManager",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractManager", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractManager_Contract_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contract",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractManager_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTimesheet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Workload = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTimesheet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectTimesheet_Contract_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contract",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectTimesheet_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceDay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendanceTimesheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClockIn = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ClockOut = table.Column<TimeSpan>(type: "interval", nullable: true),
                    BreakStart = table.Column<TimeSpan>(type: "interval", nullable: true),
                    BreakEnd = table.Column<TimeSpan>(type: "interval", nullable: true),
                    Workload = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    HoursWithoutBreak = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    HoursObligation = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    IsHoliday = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Schedules = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceDay", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceDay_AttendanceTimesheet_AttendanceTimesheetId",
                        column: x => x.AttendanceTimesheetId,
                        principalTable: "AttendanceTimesheet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectDay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectTimesheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Hours = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    IsHoliday = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Workload = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    HoursObligation = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDay", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectDay_ProjectTimesheet_ProjectTimesheetId",
                        column: x => x.ProjectTimesheetId,
                        principalTable: "ProjectTimesheet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DayInterruption",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendanceDayId = table.Column<Guid>(type: "uuid", nullable: false),
                    InterruptionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DayInterruption", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DayInterruption_AttendanceDay_AttendanceDayId",
                        column: x => x.AttendanceDayId,
                        principalTable: "AttendanceDay",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DayInterruption_Interruption_InterruptionId",
                        column: x => x.InterruptionId,
                        principalTable: "Interruption",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "EmployeeType",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), "Akademik" },
                    { new Guid("00000000-0000-0000-0000-000000000002"), "Neakademik" }
                });

            migrationBuilder.InsertData(
                table: "Interruption",
                columns: new[] { "Id", "Description", "HoursObligationOverride", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000010"), "Dovolenka", 0m, "D" },
                    { new Guid("00000000-0000-0000-0000-000000000011"), "práce na dálku od 1.10.2023", null, "JMV/HO" },
                    { new Guid("00000000-0000-0000-0000-000000000012"), "Karanténa -home office", null, "KAHO" },
                    { new Guid("00000000-0000-0000-0000-000000000013"), "Omluvená nepřítomnost - tvůrčí volno", 0m, "M" },
                    { new Guid("00000000-0000-0000-0000-000000000014"), "Mateřská dovolená / Otcovská dovolená", 0m, "MD/OD" },
                    { new Guid("00000000-0000-0000-0000-000000000015"), "Nemocenská", 0m, "N" },
                    { new Guid("00000000-0000-0000-0000-000000000016"), "Neomluvená absence", 0m, "NA" },
                    { new Guid("00000000-0000-0000-0000-000000000017"), "Návštěva lékaře - krátkodobá", null, "NK" },
                    { new Guid("00000000-0000-0000-0000-000000000018"), "Návštěva lékaře - celý den", 0m, "NL" },
                    { new Guid("00000000-0000-0000-0000-000000000019"), "Pracovní úraz", 0m, "NP" },
                    { new Guid("00000000-0000-0000-0000-000000000020"), "Náhradní volno za odprac. dobu", 0m, "NV" },
                    { new Guid("00000000-0000-0000-0000-000000000021"), "Ošetřovné", 0m, "O" },
                    { new Guid("00000000-0000-0000-0000-000000000022"), "Osobní překážky", null, "OPN" },
                    { new Guid("00000000-0000-0000-0000-000000000023"), "Narození dítěte", 0m, "PN" },
                    { new Guid("00000000-0000-0000-0000-000000000024"), "Odběr krve", null, "PO" },
                    { new Guid("00000000-0000-0000-0000-000000000025"), "Svatba", 0m, "PS" },
                    { new Guid("00000000-0000-0000-0000-000000000026"), "Úmrtí rod. příslušníka", 0m, "PU" },
                    { new Guid("00000000-0000-0000-0000-000000000027"), "Pracovní volno pro brannou povinnost", 0m, "PVB" },
                    { new Guid("00000000-0000-0000-0000-000000000028"), "Pracovní volno pro s akcí pro děti a mládež", 0m, "PVM" },
                    { new Guid("00000000-0000-0000-0000-000000000029"), "Překážka na straně zaměstnavatele", 0m, "PZ" },
                    { new Guid("00000000-0000-0000-0000-000000000030"), "Rodičovská dovolená", 0m, "RD" },
                    { new Guid("00000000-0000-0000-0000-000000000031"), "Tuzemská služební cesta Projekt", null, "SCP" },
                    { new Guid("00000000-0000-0000-0000-000000000032"), "Tuzemská služební cesta Stáž", null, "SCS" },
                    { new Guid("00000000-0000-0000-0000-000000000033"), "Služební cesta", null, "SCT" },
                    { new Guid("00000000-0000-0000-0000-000000000034"), "Služební cesta zahraniční", null, "SCZ" },
                    { new Guid("00000000-0000-0000-0000-000000000035"), "Zahraniční služební cesta Erasmus", null, "SCZE" },
                    { new Guid("00000000-0000-0000-0000-000000000036"), "Zahraniční služební cesta Projekt", null, "SCZP" },
                    { new Guid("00000000-0000-0000-0000-000000000037"), "Zahraniční služební cesta Stáž", null, "SCZS" },
                    { new Guid("00000000-0000-0000-0000-000000000038"), "Studium s náhradou mzdy", null, "ST" },
                    { new Guid("00000000-0000-0000-0000-000000000039"), "Neplacené volno", 0m, "VN" },
                    { new Guid("00000000-0000-0000-0000-000000000040"), "Nové zaměstnání", 0m, "VZ" },
                    { new Guid("00000000-0000-0000-0000-000000000041"), "Volno pro obecný zájem", 0m, "Z" },
                    { new Guid("00000000-0000-0000-0000-000000000042"), "Veřejná funkce - poslanec", null, "Zp" },
                    { new Guid("00000000-0000-0000-0000-000000000043"), "Dlouhodobý pobyt v cizině", null, "Zs" },
                    { new Guid("00000000-0000-0000-0000-000000000044"), "Zdravotní volno", 0m, "Zv" }
                });

            migrationBuilder.InsertData(
                table: "TimesheetStatus",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000020"), "Rozpracovaný" },
                    { new Guid("00000000-0000-0000-0000-000000000021"), "Ke schválení" },
                    { new Guid("00000000-0000-0000-0000-000000000022"), "Schválený" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceDay_AttendanceTimesheetId_Date",
                table: "AttendanceDay",
                columns: new[] { "AttendanceTimesheetId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceTimesheet_ApprovedBy",
                table: "AttendanceTimesheet",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceTimesheet_ContractId",
                table: "AttendanceTimesheet",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceTimesheet_EmployeeId_ContractId_Year_Month",
                table: "AttendanceTimesheet",
                columns: new[] { "EmployeeId", "ContractId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceTimesheet_TimesheetStatusId",
                table: "AttendanceTimesheet",
                column: "TimesheetStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Contract_ProjectId",
                table: "Contract",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractEmployee_ContractId_EmployeeId_Position",
                table: "ContractEmployee",
                columns: new[] { "ContractId", "EmployeeId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractEmployee_EmployeeId",
                table: "ContractEmployee",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractManager_ContractId_EmployeeId",
                table: "ContractManager",
                columns: new[] { "ContractId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractManager_EmployeeId",
                table: "ContractManager",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CoreEmployment_EmployeeId_StartDate_EndDate",
                table: "CoreEmployment",
                columns: new[] { "EmployeeId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DayInterruption_AttendanceDayId_InterruptionId",
                table: "DayInterruption",
                columns: new[] { "AttendanceDayId", "InterruptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DayInterruption_InterruptionId",
                table: "DayInterruption",
                column: "InterruptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_Email",
                table: "Employee",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employee_EmployeeTypeId",
                table: "Employee",
                column: "EmployeeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkload_EmployeeId_Year_Month",
                table: "EmployeeWorkload",
                columns: new[] { "EmployeeId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notification_EmployeeId",
                table: "Notification",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDay_ProjectTimesheetId_Date",
                table: "ProjectDay",
                columns: new[] { "ProjectTimesheetId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectManager_EmployeeId",
                table: "ProjectManager",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectManager_ProjectId_EmployeeId",
                table: "ProjectManager",
                columns: new[] { "ProjectId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTimesheet_ContractId",
                table: "ProjectTimesheet",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTimesheet_EmployeeId_Year_Month",
                table: "ProjectTimesheet",
                columns: new[] { "EmployeeId", "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractEmployee");

            migrationBuilder.DropTable(
                name: "ContractManager");

            migrationBuilder.DropTable(
                name: "CoreEmployment");

            migrationBuilder.DropTable(
                name: "DayInterruption");

            migrationBuilder.DropTable(
                name: "EmployeeWorkload");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "ProjectDay");

            migrationBuilder.DropTable(
                name: "ProjectManager");

            migrationBuilder.DropTable(
                name: "AttendanceDay");

            migrationBuilder.DropTable(
                name: "Interruption");

            migrationBuilder.DropTable(
                name: "ProjectTimesheet");

            migrationBuilder.DropTable(
                name: "AttendanceTimesheet");

            migrationBuilder.DropTable(
                name: "Contract");

            migrationBuilder.DropTable(
                name: "Employee");

            migrationBuilder.DropTable(
                name: "TimesheetStatus");

            migrationBuilder.DropTable(
                name: "Project");

            migrationBuilder.DropTable(
                name: "EmployeeType");
        }
    }
}
