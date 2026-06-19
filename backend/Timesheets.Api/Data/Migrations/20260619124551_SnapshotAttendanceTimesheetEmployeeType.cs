using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SnapshotAttendanceTimesheetEmployeeType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeTypeId",
                table: "AttendanceTimesheet",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "AttendanceTimesheet" AS timesheet
                SET "EmployeeTypeId" = employee."EmployeeTypeId"
                FROM "Employee" AS employee
                WHERE timesheet."EmployeeId" = employee."Id";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceTimesheet_EmployeeTypeId",
                table: "AttendanceTimesheet",
                column: "EmployeeTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceTimesheet_EmployeeType_EmployeeTypeId",
                table: "AttendanceTimesheet",
                column: "EmployeeTypeId",
                principalTable: "EmployeeType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceTimesheet_EmployeeType_EmployeeTypeId",
                table: "AttendanceTimesheet");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceTimesheet_EmployeeTypeId",
                table: "AttendanceTimesheet");

            migrationBuilder.DropColumn(
                name: "EmployeeTypeId",
                table: "AttendanceTimesheet");
        }
    }
}
