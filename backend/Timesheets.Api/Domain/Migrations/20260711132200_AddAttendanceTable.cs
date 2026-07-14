using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TimesheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeTypeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attendance_EmployeeType_EmployeeTypeId",
                        column: x => x.EmployeeTypeId,
                        principalTable: "EmployeeType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Attendance_Timesheet_TimesheetId",
                        column: x => x.TimesheetId,
                        principalTable: "Timesheet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_EmployeeTypeId",
                table: "Attendance",
                column: "EmployeeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_TimesheetId",
                table: "Attendance",
                column: "TimesheetId",
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO "Attendance" ("Id", "TimesheetId", "EmployeeTypeId")
                SELECT
                    t."Id",
                    t."Id",
                    COALESCE(at."EmployeeTypeId", e."EmployeeTypeId")
                FROM "Timesheet" t
                INNER JOIN "AttendanceTimesheet" at ON at."Id" = t."Id"
                INNER JOIN "Employee" e ON e."Id" = t."EmployeeId";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attendance");
        }
    }
}
