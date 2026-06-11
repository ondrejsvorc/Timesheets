using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectTimesheetColumnLock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAt",
                table: "ProjectTimesheet",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LockedBy",
                table: "ProjectTimesheet",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LockedAt",
                table: "ProjectTimesheet");

            migrationBuilder.DropColumn(
                name: "LockedBy",
                table: "ProjectTimesheet");
        }
    }
}
