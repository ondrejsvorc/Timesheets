using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddTimesheetStatusCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "TimesheetStatus",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "TimesheetStatus",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000020"),
                column: "Code",
                value: "DRAFT");

            migrationBuilder.UpdateData(
                table: "TimesheetStatus",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000021"),
                column: "Code",
                value: "SUBMITTED");

            migrationBuilder.UpdateData(
                table: "TimesheetStatus",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000022"),
                column: "Code",
                value: "APPROVED");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetStatus_Code",
                table: "TimesheetStatus",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TimesheetStatus_Code",
                table: "TimesheetStatus");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "TimesheetStatus");
        }
    }
}
