using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddTimesheetStatusHistoryAndComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TimesheetComment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendanceTimesheetId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectTimesheetId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimesheetComment", x => x.Id);
                    table.CheckConstraint("CK_TimesheetComment_ExactlyOneTimesheet", "(\"AttendanceTimesheetId\" IS NOT NULL AND \"ProjectTimesheetId\" IS NULL)\r\nOR\r\n(\"AttendanceTimesheetId\" IS NULL AND \"ProjectTimesheetId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_TimesheetComment_AttendanceTimesheet_AttendanceTimesheetId",
                        column: x => x.AttendanceTimesheetId,
                        principalTable: "AttendanceTimesheet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimesheetComment_Employee_AuthorEmployeeId",
                        column: x => x.AuthorEmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimesheetComment_ProjectTimesheet_ProjectTimesheetId",
                        column: x => x.ProjectTimesheetId,
                        principalTable: "ProjectTimesheet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimesheetStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendanceTimesheetId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectTimesheetId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromStatusId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToStatusId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimesheetStatusHistory", x => x.Id);
                    table.CheckConstraint("CK_TimesheetStatusHistory_ExactlyOneTimesheet", "(\"AttendanceTimesheetId\" IS NOT NULL AND \"ProjectTimesheetId\" IS NULL)\r\nOR\r\n(\"AttendanceTimesheetId\" IS NULL AND \"ProjectTimesheetId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_TimesheetStatusHistory_AttendanceTimesheet_AttendanceTimesh~",
                        column: x => x.AttendanceTimesheetId,
                        principalTable: "AttendanceTimesheet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimesheetStatusHistory_Employee_ChangedByEmployeeId",
                        column: x => x.ChangedByEmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimesheetStatusHistory_ProjectTimesheet_ProjectTimesheetId",
                        column: x => x.ProjectTimesheetId,
                        principalTable: "ProjectTimesheet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimesheetStatusHistory_TimesheetStatus_FromStatusId",
                        column: x => x.FromStatusId,
                        principalTable: "TimesheetStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimesheetStatusHistory_TimesheetStatus_ToStatusId",
                        column: x => x.ToStatusId,
                        principalTable: "TimesheetStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetComment_AttendanceTimesheetId",
                table: "TimesheetComment",
                column: "AttendanceTimesheetId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetComment_AuthorEmployeeId",
                table: "TimesheetComment",
                column: "AuthorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetComment_CreatedAt",
                table: "TimesheetComment",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetComment_ProjectTimesheetId",
                table: "TimesheetComment",
                column: "ProjectTimesheetId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetStatusHistory_AttendanceTimesheetId",
                table: "TimesheetStatusHistory",
                column: "AttendanceTimesheetId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetStatusHistory_FromStatusId",
                table: "TimesheetStatusHistory",
                column: "FromStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetStatusHistory_ChangedAt",
                table: "TimesheetStatusHistory",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetStatusHistory_ChangedByEmployeeId",
                table: "TimesheetStatusHistory",
                column: "ChangedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetStatusHistory_ProjectTimesheetId",
                table: "TimesheetStatusHistory",
                column: "ProjectTimesheetId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetStatusHistory_ToStatusId",
                table: "TimesheetStatusHistory",
                column: "ToStatusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TimesheetComment");

            migrationBuilder.DropTable(
                name: "TimesheetStatusHistory");
        }
    }
}
