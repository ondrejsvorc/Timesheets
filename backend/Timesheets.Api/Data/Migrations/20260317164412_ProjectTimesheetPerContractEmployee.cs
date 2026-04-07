using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Timesheets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProjectTimesheetPerContractEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectTimesheet_EmployeeId_ContractId_Year_Month",
                table: "ProjectTimesheet");

            migrationBuilder.AddColumn<Guid>(
                name: "ContractEmployeeId",
                table: "ProjectTimesheet",
                type: "uuid",
                nullable: true);

            // Backfill ContractEmployeeId for existing rows by matching EmployeeId+ContractId and overlapping month.
            migrationBuilder.Sql(
                """
                UPDATE "ProjectTimesheet" pt
                SET "ContractEmployeeId" = ce."Id"
                FROM "ContractEmployee" ce
                WHERE ce."EmployeeId" = pt."EmployeeId"
                  AND ce."ContractId" = pt."ContractId"
                  AND ce."StartDate" <= (make_date(pt."Year", pt."Month", 1) + interval '1 month' - interval '1 day')
                  AND (ce."EndDate" IS NULL OR ce."EndDate" >= make_date(pt."Year", pt."Month", 1));
                """
            );

            // Fallback 1: if there are multiple assignments or date ranges don't match, pick any ContractEmployee for employee+contract.
            migrationBuilder.Sql(
                """
                UPDATE "ProjectTimesheet" pt
                SET "ContractEmployeeId" = (
                  SELECT ce."Id"
                  FROM "ContractEmployee" ce
                  WHERE ce."EmployeeId" = pt."EmployeeId"
                    AND ce."ContractId" = pt."ContractId"
                  ORDER BY ce."StartDate" DESC
                  LIMIT 1
                )
                WHERE pt."ContractEmployeeId" IS NULL;
                """
            );

            // Fallback 2: create a legacy ContractEmployee for any remaining employee+contract pairs and attach.
            migrationBuilder.Sql(
                """
                WITH pairs AS (
                  SELECT DISTINCT pt."EmployeeId", pt."ContractId"
                  FROM "ProjectTimesheet" pt
                  WHERE pt."ContractEmployeeId" IS NULL
                ),
                ins AS (
                  INSERT INTO "ContractEmployee"
                    ("Id", "ContractId", "EmployeeId", "PositionCode", "Position", "Workload", "StartDate", "EndDate")
                  SELECT
                    (
                      substring(md5(p."EmployeeId"::text || p."ContractId"::text) from 1 for 8) || '-' ||
                      substring(md5(p."EmployeeId"::text || p."ContractId"::text) from 9 for 4) || '-' ||
                      substring(md5(p."EmployeeId"::text || p."ContractId"::text) from 13 for 4) || '-' ||
                      substring(md5(p."EmployeeId"::text || p."ContractId"::text) from 17 for 4) || '-' ||
                      substring(md5(p."EmployeeId"::text || p."ContractId"::text) from 21 for 12)
                    )::uuid,
                    p."ContractId",
                    p."EmployeeId",
                    'LEGACY',
                    'Legacy',
                    0.0,
                    TIMESTAMPTZ '1900-01-01 00:00:00+00',
                    NULL
                  FROM pairs p
                  WHERE NOT EXISTS (
                    SELECT 1 FROM "ContractEmployee" ce
                    WHERE ce."EmployeeId" = p."EmployeeId" AND ce."ContractId" = p."ContractId"
                  )
                  RETURNING "Id", "EmployeeId", "ContractId"
                )
                UPDATE "ProjectTimesheet" pt
                SET "ContractEmployeeId" = ce."Id"
                FROM "ContractEmployee" ce
                WHERE pt."ContractEmployeeId" IS NULL
                  AND ce."EmployeeId" = pt."EmployeeId"
                  AND ce."ContractId" = pt."ContractId";
                """
            );

            // Final guard: ContractEmployeeId must be backfilled for all rows.
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                  IF EXISTS (SELECT 1 FROM "ProjectTimesheet" WHERE "ContractEmployeeId" IS NULL) THEN
                    RAISE EXCEPTION 'ProjectTimesheet.ContractEmployeeId backfill failed for some rows.';
                  END IF;
                END $$;
                """
            );

            migrationBuilder.AlterColumn<Guid>(
                name: "ContractEmployeeId",
                table: "ProjectTimesheet",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTimesheet_ContractEmployeeId_Year_Month",
                table: "ProjectTimesheet",
                columns: new[] { "ContractEmployeeId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTimesheet_EmployeeId",
                table: "ProjectTimesheet",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTimesheet_ContractEmployee_ContractEmployeeId",
                table: "ProjectTimesheet",
                column: "ContractEmployeeId",
                principalTable: "ContractEmployee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTimesheet_ContractEmployee_ContractEmployeeId",
                table: "ProjectTimesheet");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTimesheet_ContractEmployeeId_Year_Month",
                table: "ProjectTimesheet");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTimesheet_EmployeeId",
                table: "ProjectTimesheet");

            migrationBuilder.DropColumn(
                name: "ContractEmployeeId",
                table: "ProjectTimesheet");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTimesheet_EmployeeId_ContractId_Year_Month",
                table: "ProjectTimesheet",
                columns: new[] { "EmployeeId", "ContractId", "Year", "Month" },
                unique: true);
        }
    }
}
