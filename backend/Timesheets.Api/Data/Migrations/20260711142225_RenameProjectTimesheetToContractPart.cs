using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameProjectTimesheetToContractPart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimesheetComment_ProjectTimesheet_ProjectTimesheetId",
                table: "TimesheetComment");

            migrationBuilder.DropForeignKey(
                name: "FK_TimesheetStatusHistory_ProjectTimesheet_ProjectTimesheetId",
                table: "TimesheetStatusHistory");

            migrationBuilder.DropTable(
                name: "ProjectDay");

            migrationBuilder.DropTable(
                name: "ProjectTimesheet");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TimesheetStatusHistory_ExactlyOneTimesheet",
                table: "TimesheetStatusHistory");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TimesheetComment_ExactlyOneTimesheet",
                table: "TimesheetComment");

            migrationBuilder.RenameColumn(
                name: "ProjectTimesheetId",
                table: "TimesheetStatusHistory",
                newName: "ContractPartId");

            migrationBuilder.RenameIndex(
                name: "IX_TimesheetStatusHistory_ProjectTimesheetId",
                table: "TimesheetStatusHistory",
                newName: "IX_TimesheetStatusHistory_ContractPartId");

            migrationBuilder.RenameColumn(
                name: "ProjectTimesheetId",
                table: "TimesheetComment",
                newName: "ContractPartId");

            migrationBuilder.RenameIndex(
                name: "IX_TimesheetComment_ProjectTimesheetId",
                table: "TimesheetComment",
                newName: "IX_TimesheetComment_ContractPartId");

            migrationBuilder.CreateTable(
                name: "ContractPart",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TimesheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimesheetStatusId = table.Column<Guid>(type: "uuid", nullable: false),
                    Workload = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    LockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractPart", x => x.Id);
                    table.CheckConstraint("CK_ContractPart_Lock", "(\"LockedAt\" IS NULL AND \"LockedBy\" IS NULL)\r\nOR\r\n(\"LockedAt\" IS NOT NULL AND \"LockedBy\" IS NOT NULL)");
                    table.CheckConstraint("CK_ContractPart_WorkloadRange", "\"Workload\" >= 0 AND \"Workload\" <= 1");
                    table.ForeignKey(
                        name: "FK_ContractPart_ContractEmployee_ContractEmployeeId",
                        column: x => x.ContractEmployeeId,
                        principalTable: "ContractEmployee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractPart_TimesheetStatus_TimesheetStatusId",
                        column: x => x.TimesheetStatusId,
                        principalTable: "TimesheetStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractPart_Timesheet_TimesheetId",
                        column: x => x.TimesheetId,
                        principalTable: "Timesheet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractPartDay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractPartId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Hours = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    HoursLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsHoliday = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    HoursObligation = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractPartDay", x => x.Id);
                    table.CheckConstraint("CK_ContractPartDay_Hours", "\"Hours\" >= 0");
                    table.CheckConstraint("CK_ContractPartDay_HoursObligation", "\"HoursObligation\" >= 0");
                    table.ForeignKey(
                        name: "FK_ContractPartDay_ContractPart_ContractPartId",
                        column: x => x.ContractPartId,
                        principalTable: "ContractPart",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_TimesheetStatusHistory_ExactlyOneTimesheet",
                table: "TimesheetStatusHistory",
                sql: "(\"TimesheetId\" IS NOT NULL AND \"ContractPartId\" IS NULL)\r\nOR\r\n(\"TimesheetId\" IS NULL AND \"ContractPartId\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TimesheetComment_ExactlyOneTimesheet",
                table: "TimesheetComment",
                sql: "(\"TimesheetId\" IS NOT NULL AND \"ContractPartId\" IS NULL)\r\nOR\r\n(\"TimesheetId\" IS NULL AND \"ContractPartId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_ContractPart_ContractEmployeeId",
                table: "ContractPart",
                column: "ContractEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractPart_TimesheetId_ContractEmployeeId",
                table: "ContractPart",
                columns: new[] { "TimesheetId", "ContractEmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractPart_TimesheetStatusId",
                table: "ContractPart",
                column: "TimesheetStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractPartDay_ContractPartId_Date",
                table: "ContractPartDay",
                columns: new[] { "ContractPartId", "Date" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TimesheetComment_ContractPart_ContractPartId",
                table: "TimesheetComment",
                column: "ContractPartId",
                principalTable: "ContractPart",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TimesheetStatusHistory_ContractPart_ContractPartId",
                table: "TimesheetStatusHistory",
                column: "ContractPartId",
                principalTable: "ContractPart",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimesheetComment_ContractPart_ContractPartId",
                table: "TimesheetComment");

            migrationBuilder.DropForeignKey(
                name: "FK_TimesheetStatusHistory_ContractPart_ContractPartId",
                table: "TimesheetStatusHistory");

            migrationBuilder.DropTable(
                name: "ContractPartDay");

            migrationBuilder.DropTable(
                name: "ContractPart");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TimesheetStatusHistory_ExactlyOneTimesheet",
                table: "TimesheetStatusHistory");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TimesheetComment_ExactlyOneTimesheet",
                table: "TimesheetComment");

            migrationBuilder.RenameColumn(
                name: "ContractPartId",
                table: "TimesheetStatusHistory",
                newName: "ProjectTimesheetId");

            migrationBuilder.RenameIndex(
                name: "IX_TimesheetStatusHistory_ContractPartId",
                table: "TimesheetStatusHistory",
                newName: "IX_TimesheetStatusHistory_ProjectTimesheetId");

            migrationBuilder.RenameColumn(
                name: "ContractPartId",
                table: "TimesheetComment",
                newName: "ProjectTimesheetId");

            migrationBuilder.RenameIndex(
                name: "IX_TimesheetComment_ContractPartId",
                table: "TimesheetComment",
                newName: "IX_TimesheetComment_ProjectTimesheetId");

            migrationBuilder.CreateTable(
                name: "ProjectTimesheet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TimesheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimesheetStatusId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Workload = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTimesheet", x => x.Id);
                    table.CheckConstraint("CK_ProjectTimesheet_ValidMonth", "\"Month\" >= 1 AND \"Month\" <= 12");
                    table.CheckConstraint("CK_ProjectTimesheet_WorkloadRange", "\"Workload\" >= 0 AND \"Workload\" <= 1");
                    table.ForeignKey(
                        name: "FK_ProjectTimesheet_ContractEmployee_ContractEmployeeId",
                        column: x => x.ContractEmployeeId,
                        principalTable: "ContractEmployee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    table.ForeignKey(
                        name: "FK_ProjectTimesheet_TimesheetStatus_TimesheetStatusId",
                        column: x => x.TimesheetStatusId,
                        principalTable: "TimesheetStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectTimesheet_Timesheet_TimesheetId",
                        column: x => x.TimesheetId,
                        principalTable: "Timesheet",
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
                    HoursLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    HoursObligation = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    IsHoliday = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Workload = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDay", x => x.Id);
                    table.CheckConstraint("CK_ProjectDay_HoursAndWorkload", "\"Hours\" >= 0\r\nAND \"Workload\" >= 0 AND \"Workload\" <= 1\r\nAND \"HoursObligation\" >= 0");
                    table.ForeignKey(
                        name: "FK_ProjectDay_ProjectTimesheet_ProjectTimesheetId",
                        column: x => x.ProjectTimesheetId,
                        principalTable: "ProjectTimesheet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_TimesheetStatusHistory_ExactlyOneTimesheet",
                table: "TimesheetStatusHistory",
                sql: "(\"TimesheetId\" IS NOT NULL AND \"ProjectTimesheetId\" IS NULL)\r\nOR\r\n(\"TimesheetId\" IS NULL AND \"ProjectTimesheetId\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TimesheetComment_ExactlyOneTimesheet",
                table: "TimesheetComment",
                sql: "(\"TimesheetId\" IS NOT NULL AND \"ProjectTimesheetId\" IS NULL)\r\nOR\r\n(\"TimesheetId\" IS NULL AND \"ProjectTimesheetId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDay_ProjectTimesheetId_Date",
                table: "ProjectDay",
                columns: new[] { "ProjectTimesheetId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTimesheet_ContractEmployeeId_Year_Month",
                table: "ProjectTimesheet",
                columns: new[] { "ContractEmployeeId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTimesheet_ContractId",
                table: "ProjectTimesheet",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTimesheet_EmployeeId",
                table: "ProjectTimesheet",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTimesheet_TimesheetId_ContractEmployeeId",
                table: "ProjectTimesheet",
                columns: new[] { "TimesheetId", "ContractEmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTimesheet_TimesheetStatusId",
                table: "ProjectTimesheet",
                column: "TimesheetStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimesheetComment_ProjectTimesheet_ProjectTimesheetId",
                table: "TimesheetComment",
                column: "ProjectTimesheetId",
                principalTable: "ProjectTimesheet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TimesheetStatusHistory_ProjectTimesheet_ProjectTimesheetId",
                table: "TimesheetStatusHistory",
                column: "ProjectTimesheetId",
                principalTable: "ProjectTimesheet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
