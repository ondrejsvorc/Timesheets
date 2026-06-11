using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RequirePositionWorkloadAndRemoveAttendanceContractId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceTimesheet_Contract_ContractId",
                table: "AttendanceTimesheet");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceTimesheet_ContractId",
                table: "AttendanceTimesheet");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceTimesheet_EmployeeId_ContractId_Year_Month",
                table: "AttendanceTimesheet");

            migrationBuilder.DropColumn(
                name: "ContractId",
                table: "AttendanceTimesheet");

            migrationBuilder.AlterColumn<decimal>(
                name: "Workload",
                table: "ContractEmployee",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Position",
                table: "ContractEmployee",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Workload",
                table: "AttendanceDay",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceTimesheet_EmployeeId_Year_Month",
                table: "AttendanceTimesheet",
                columns: new[] { "EmployeeId", "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AttendanceTimesheet_EmployeeId_Year_Month",
                table: "AttendanceTimesheet");

            migrationBuilder.AlterColumn<decimal>(
                name: "Workload",
                table: "ContractEmployee",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Position",
                table: "ContractEmployee",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<Guid>(
                name: "ContractId",
                table: "AttendanceTimesheet",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<decimal>(
                name: "Workload",
                table: "AttendanceDay",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceTimesheet_ContractId",
                table: "AttendanceTimesheet",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceTimesheet_EmployeeId_ContractId_Year_Month",
                table: "AttendanceTimesheet",
                columns: new[] { "EmployeeId", "ContractId", "Year", "Month" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceTimesheet_Contract_ContractId",
                table: "AttendanceTimesheet",
                column: "ContractId",
                principalTable: "Contract",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
