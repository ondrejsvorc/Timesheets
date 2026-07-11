using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropAttendanceTimesheet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimesheetComment_AttendanceTimesheet_AttendanceTimesheetId",
                table: "TimesheetComment");

            migrationBuilder.DropForeignKey(
                name: "FK_TimesheetStatusHistory_AttendanceTimesheet_AttendanceTimesh~",
                table: "TimesheetStatusHistory");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TimesheetStatusHistory_ExactlyOneTimesheet",
                table: "TimesheetStatusHistory");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TimesheetComment_ExactlyOneTimesheet",
                table: "TimesheetComment");

            migrationBuilder.RenameColumn(
                name: "AttendanceTimesheetId",
                table: "TimesheetStatusHistory",
                newName: "TimesheetId");

            migrationBuilder.RenameIndex(
                name: "IX_TimesheetStatusHistory_AttendanceTimesheetId",
                table: "TimesheetStatusHistory",
                newName: "IX_TimesheetStatusHistory_TimesheetId");

            migrationBuilder.RenameColumn(
                name: "AttendanceTimesheetId",
                table: "TimesheetComment",
                newName: "TimesheetId");

            migrationBuilder.RenameIndex(
                name: "IX_TimesheetComment_AttendanceTimesheetId",
                table: "TimesheetComment",
                newName: "IX_TimesheetComment_TimesheetId");

            migrationBuilder.DropTable(
                name: "AttendanceTimesheet");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TimesheetStatusHistory_ExactlyOneTimesheet",
                table: "TimesheetStatusHistory",
                sql: """
                ("TimesheetId" IS NOT NULL AND "ProjectTimesheetId" IS NULL)
                OR
                ("TimesheetId" IS NULL AND "ProjectTimesheetId" IS NOT NULL)
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_TimesheetComment_ExactlyOneTimesheet",
                table: "TimesheetComment",
                sql: """
                ("TimesheetId" IS NOT NULL AND "ProjectTimesheetId" IS NULL)
                OR
                ("TimesheetId" IS NULL AND "ProjectTimesheetId" IS NOT NULL)
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_TimesheetComment_Timesheet_TimesheetId",
                table: "TimesheetComment",
                column: "TimesheetId",
                principalTable: "Timesheet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TimesheetStatusHistory_Timesheet_TimesheetId",
                table: "TimesheetStatusHistory",
                column: "TimesheetId",
                principalTable: "Timesheet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimesheetComment_Timesheet_TimesheetId",
                table: "TimesheetComment");

            migrationBuilder.DropForeignKey(
                name: "FK_TimesheetStatusHistory_Timesheet_TimesheetId",
                table: "TimesheetStatusHistory");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TimesheetStatusHistory_ExactlyOneTimesheet",
                table: "TimesheetStatusHistory");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TimesheetComment_ExactlyOneTimesheet",
                table: "TimesheetComment");

            migrationBuilder.CreateTable(
                name: "AttendanceTimesheet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimesheetStatusId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmployeeTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceTimesheet", x => x.Id);
                    table.CheckConstraint("CK_AttendanceTimesheet_ValidMonth", "\"Month\" >= 1 AND \"Month\" <= 12");
                    table.ForeignKey(
                        name: "FK_AttendanceTimesheet_EmployeeType_EmployeeTypeId",
                        column: x => x.EmployeeTypeId,
                        principalTable: "EmployeeType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.RenameColumn(
                name: "TimesheetId",
                table: "TimesheetStatusHistory",
                newName: "AttendanceTimesheetId");

            migrationBuilder.RenameIndex(
                name: "IX_TimesheetStatusHistory_TimesheetId",
                table: "TimesheetStatusHistory",
                newName: "IX_TimesheetStatusHistory_AttendanceTimesheetId");

            migrationBuilder.RenameColumn(
                name: "TimesheetId",
                table: "TimesheetComment",
                newName: "AttendanceTimesheetId");

            migrationBuilder.RenameIndex(
                name: "IX_TimesheetComment_TimesheetId",
                table: "TimesheetComment",
                newName: "IX_TimesheetComment_AttendanceTimesheetId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceTimesheet_ApprovedBy",
                table: "AttendanceTimesheet",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceTimesheet_EmployeeId_Year_Month",
                table: "AttendanceTimesheet",
                columns: new[] { "EmployeeId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceTimesheet_EmployeeTypeId",
                table: "AttendanceTimesheet",
                column: "EmployeeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceTimesheet_TimesheetStatusId",
                table: "AttendanceTimesheet",
                column: "TimesheetStatusId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TimesheetStatusHistory_ExactlyOneTimesheet",
                table: "TimesheetStatusHistory",
                sql: "(\"AttendanceTimesheetId\" IS NOT NULL AND \"ProjectTimesheetId\" IS NULL)\r\nOR\r\n(\"AttendanceTimesheetId\" IS NULL AND \"ProjectTimesheetId\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TimesheetComment_ExactlyOneTimesheet",
                table: "TimesheetComment",
                sql: "(\"AttendanceTimesheetId\" IS NOT NULL AND \"ProjectTimesheetId\" IS NULL)\r\nOR\r\n(\"AttendanceTimesheetId\" IS NULL AND \"ProjectTimesheetId\" IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_TimesheetComment_AttendanceTimesheet_AttendanceTimesheetId",
                table: "TimesheetComment",
                column: "AttendanceTimesheetId",
                principalTable: "AttendanceTimesheet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TimesheetStatusHistory_AttendanceTimesheet_AttendanceTimesh~",
                table: "TimesheetStatusHistory",
                column: "AttendanceTimesheetId",
                principalTable: "AttendanceTimesheet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
