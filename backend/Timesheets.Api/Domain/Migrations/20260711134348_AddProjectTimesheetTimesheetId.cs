using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectTimesheetTimesheetId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TimesheetId",
                table: "ProjectTimesheet",
                type: "uuid",
                nullable: true);

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

                INSERT INTO "Timesheet" (
                    "Id",
                    "EmployeeId",
                    "TimesheetStatusId",
                    "Year",
                    "Month",
                    "CreatedAt")
                SELECT
                    gen_random_uuid(),
                    pt."EmployeeId",
                    '00000000-0000-0000-0000-000000000020',
                    pt."Year",
                    pt."Month",
                    NOW() AT TIME ZONE 'UTC'
                FROM (
                    SELECT DISTINCT pt."EmployeeId", pt."Year", pt."Month"
                    FROM "ProjectTimesheet" pt
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM "Timesheet" t
                        WHERE t."EmployeeId" = pt."EmployeeId"
                          AND t."Year" = pt."Year"
                          AND t."Month" = pt."Month")
                ) pt;

                UPDATE "ProjectTimesheet" pt
                SET "TimesheetId" = t."Id"
                FROM "Timesheet" t
                WHERE t."EmployeeId" = pt."EmployeeId"
                  AND t."Year" = pt."Year"
                  AND t."Month" = pt."Month"
                  AND pt."TimesheetId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "TimesheetId",
                table: "ProjectTimesheet",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTimesheet_TimesheetId_ContractEmployeeId",
                table: "ProjectTimesheet",
                columns: new[] { "TimesheetId", "ContractEmployeeId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTimesheet_Timesheet_TimesheetId",
                table: "ProjectTimesheet",
                column: "TimesheetId",
                principalTable: "Timesheet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTimesheet_Timesheet_TimesheetId",
                table: "ProjectTimesheet");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTimesheet_TimesheetId_ContractEmployeeId",
                table: "ProjectTimesheet");

            migrationBuilder.DropColumn(
                name: "TimesheetId",
                table: "ProjectTimesheet");
        }
    }
}
