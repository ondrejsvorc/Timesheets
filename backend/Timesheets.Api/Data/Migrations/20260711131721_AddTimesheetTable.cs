using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTimesheetTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Timesheet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_Timesheet", x => x.Id);
                    table.CheckConstraint("CK_Timesheet_ValidMonth", "\"Month\" >= 1 AND \"Month\" <= 12");
                    table.ForeignKey(
                        name: "FK_Timesheet_Employee_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Timesheet_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Timesheet_TimesheetStatus_TimesheetStatusId",
                        column: x => x.TimesheetStatusId,
                        principalTable: "TimesheetStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_ApprovedBy",
                table: "Timesheet",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_EmployeeId_Year_Month",
                table: "Timesheet",
                columns: new[] { "EmployeeId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Timesheet_TimesheetStatusId",
                table: "Timesheet",
                column: "TimesheetStatusId");

            migrationBuilder.Sql(
                """
                INSERT INTO "Timesheet" (
                    "Id",
                    "EmployeeId",
                    "TimesheetStatusId",
                    "ApprovedBy",
                    "Year",
                    "Month",
                    "SubmittedAt",
                    "ApprovedAt",
                    "CreatedAt",
                    "UpdatedAt")
                SELECT
                    "Id",
                    "EmployeeId",
                    "TimesheetStatusId",
                    "ApprovedBy",
                    "Year",
                    "Month",
                    "SubmittedAt",
                    "ApprovedAt",
                    "CreatedAt",
                    "UpdatedAt"
                FROM "AttendanceTimesheet";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Timesheet");
        }
    }
}
