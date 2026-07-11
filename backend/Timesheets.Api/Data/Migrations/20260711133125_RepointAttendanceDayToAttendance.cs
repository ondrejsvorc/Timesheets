using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RepointAttendanceDayToAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    at."Id",
                    at."EmployeeId",
                    at."TimesheetStatusId",
                    at."ApprovedBy",
                    at."Year",
                    at."Month",
                    at."SubmittedAt",
                    at."ApprovedAt",
                    at."CreatedAt",
                    at."UpdatedAt"
                FROM "AttendanceTimesheet" at
                WHERE NOT EXISTS (SELECT 1 FROM "Timesheet" t WHERE t."Id" = at."Id");

                INSERT INTO "Attendance" ("Id", "TimesheetId", "EmployeeTypeId")
                SELECT
                    at."Id",
                    at."Id",
                    COALESCE(at."EmployeeTypeId", e."EmployeeTypeId")
                FROM "AttendanceTimesheet" at
                INNER JOIN "Employee" e ON e."Id" = at."EmployeeId"
                WHERE NOT EXISTS (SELECT 1 FROM "Attendance" a WHERE a."Id" = at."Id");
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceDay_AttendanceTimesheet_AttendanceTimesheetId",
                table: "AttendanceDay");

            migrationBuilder.RenameColumn(
                name: "AttendanceTimesheetId",
                table: "AttendanceDay",
                newName: "AttendanceId");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceDay_AttendanceTimesheetId_Date",
                table: "AttendanceDay",
                newName: "IX_AttendanceDay_AttendanceId_Date");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceDay_Attendance_AttendanceId",
                table: "AttendanceDay",
                column: "AttendanceId",
                principalTable: "Attendance",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceDay_Attendance_AttendanceId",
                table: "AttendanceDay");

            migrationBuilder.RenameColumn(
                name: "AttendanceId",
                table: "AttendanceDay",
                newName: "AttendanceTimesheetId");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceDay_AttendanceId_Date",
                table: "AttendanceDay",
                newName: "IX_AttendanceDay_AttendanceTimesheetId_Date");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceDay_AttendanceTimesheet_AttendanceTimesheetId",
                table: "AttendanceDay",
                column: "AttendanceTimesheetId",
                principalTable: "AttendanceTimesheet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
