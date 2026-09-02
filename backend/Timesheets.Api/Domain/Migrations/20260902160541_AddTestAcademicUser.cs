using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddTestAcademicUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO "Employee" ("Id", "EmployeeTypeId", "PersonalNumber", "FirstName", "Surname", "IsGlobalManager", "CreatedAt")
                VALUES (
                    UUID '10000000-0000-0000-0000-000000000004',
                    UUID '00000000-0000-0000-0000-000000000001',
                    '3001',
                    'Testovací',
                    'Akademik',
                    FALSE,
                    TIMESTAMPTZ '2026-01-01 08:00:00+00'
                )
                ON CONFLICT DO NOTHING;

                INSERT INTO "CoreEmployment" ("Id", "EmployeeId", "Workload", "StartDate")
                SELECT
                    UUID 'C0000000-0000-0000-0000-000000000001',
                    employee."Id",
                    1.0,
                    TIMESTAMPTZ '2026-01-01 00:00:00+00'
                FROM "Employee" employee
                WHERE employee."PersonalNumber" = '3001'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "CoreEmployment" existing
                      WHERE existing."EmployeeId" = employee."Id"
                        AND existing."StartDate" = TIMESTAMPTZ '2026-01-01 00:00:00+00'
                  )
                ON CONFLICT DO NOTHING;

                INSERT INTO "Timesheet" ("Id", "EmployeeId", "TimesheetStatusId", "Year", "Month", "CreatedAt")
                SELECT
                    UUID '70000000-0000-0000-0000-000000000004',
                    employee."Id",
                    UUID '00000000-0000-0000-0000-000000000020',
                    2026,
                    1,
                    TIMESTAMPTZ '2026-01-01 08:00:00+00'
                FROM "Employee" employee
                WHERE employee."PersonalNumber" = '3001'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "Timesheet" existing
                      WHERE existing."EmployeeId" = employee."Id"
                        AND existing."Year" = 2026
                        AND existing."Month" = 1
                  )
                ON CONFLICT DO NOTHING;

                INSERT INTO "Attendance" ("Id", "TimesheetId", "EmployeeTypeId")
                SELECT
                    timesheet."Id",
                    timesheet."Id",
                    UUID '00000000-0000-0000-0000-000000000001'
                FROM "Timesheet" timesheet
                JOIN "Employee" employee ON employee."Id" = timesheet."EmployeeId"
                WHERE employee."PersonalNumber" = '3001'
                  AND timesheet."Year" = 2026
                  AND timesheet."Month" = 1
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "Attendance" existing
                      WHERE existing."TimesheetId" = timesheet."Id"
                  )
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "Attendance"
                WHERE "TimesheetId" = UUID '70000000-0000-0000-0000-000000000004';

                DELETE FROM "Timesheet"
                WHERE "Id" = UUID '70000000-0000-0000-0000-000000000004'
                  AND "EmployeeId" IN (
                      SELECT "Id"
                      FROM "Employee"
                      WHERE "PersonalNumber" = '3001'
                  );

                DELETE FROM "CoreEmployment"
                WHERE "Id" = UUID 'C0000000-0000-0000-0000-000000000001'
                  AND "EmployeeId" IN (
                      SELECT "Id"
                      FROM "Employee"
                      WHERE "PersonalNumber" = '3001'
                  );

                DELETE FROM "Employee" employee
                WHERE employee."Id" = UUID '10000000-0000-0000-0000-000000000004'
                  AND employee."PersonalNumber" = '3001'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "Timesheet" timesheet
                      WHERE timesheet."EmployeeId" = employee."Id"
                  )
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "ContractEmployee" contractEmployee
                      WHERE contractEmployee."EmployeeId" = employee."Id"
                  )
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "ProjectManager" projectManager
                      WHERE projectManager."EmployeeId" = employee."Id"
                  )
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "ContractManager" contractManager
                      WHERE contractManager."EmployeeId" = employee."Id"
                  );
                """);
        }
    }
}
