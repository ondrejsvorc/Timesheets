using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectTimesheetStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TimesheetStatusId",
                table: "ProjectTimesheet",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000020"));

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTimesheet_TimesheetStatusId",
                table: "ProjectTimesheet",
                column: "TimesheetStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTimesheet_TimesheetStatus_TimesheetStatusId",
                table: "ProjectTimesheet",
                column: "TimesheetStatusId",
                principalTable: "TimesheetStatus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTimesheet_TimesheetStatus_TimesheetStatusId",
                table: "ProjectTimesheet");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTimesheet_TimesheetStatusId",
                table: "ProjectTimesheet");

            migrationBuilder.DropColumn(
                name: "TimesheetStatusId",
                table: "ProjectTimesheet");
        }
    }
}
