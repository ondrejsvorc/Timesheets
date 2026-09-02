using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Domain.Migrations
{
    /// <inheritdoc />
    public partial class BackfillTestAcademicJanuaryAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO "EmployeeWorkload" ("Id", "EmployeeId", "Year", "Month", "Workload")
                SELECT
                    UUID 'D0000000-0000-0000-0000-000000000001',
                    employee."Id",
                    2026,
                    1,
                    1.0
                FROM "Employee" employee
                WHERE employee."PersonalNumber" = '3001'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "EmployeeWorkload" existing
                      WHERE existing."EmployeeId" = employee."Id"
                        AND existing."Year" = 2026
                        AND existing."Month" = 1
                  )
                ON CONFLICT DO NOTHING;

                INSERT INTO "Attendance" ("Id", "TimesheetId", "EmployeeTypeId")
                SELECT
                    timesheet."Id",
                    timesheet."Id",
                    employee."EmployeeTypeId"
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

                INSERT INTO "AttendanceDay" (
                    "Id",
                    "AttendanceId",
                    "Date",
                    "Workload",
                    "HoursWithoutBreak",
                    "HoursObligation",
                    "CoreHours",
                    "IsHoliday",
                    "Schedules"
                )
                SELECT
                    ('80000000-0000-0000-0001-' || lpad(day_number::text, 12, '0'))::uuid,
                    attendance."Id",
                    TIMESTAMPTZ '2026-01-01 00:00:00+00' + ((day_number - 1) * INTERVAL '1 day'),
                    1.0,
                    0.0,
                    CASE
                        WHEN day_number = 1 THEN 0.0
                        WHEN EXTRACT(ISODOW FROM TIMESTAMPTZ '2026-01-01 00:00:00+00' + ((day_number - 1) * INTERVAL '1 day')) IN (6, 7) THEN 0.0
                        ELSE 8.0
                    END,
                    0.0,
                    day_number = 1,
                    '[]'::jsonb
                FROM generate_series(1, 31) AS days(day_number)
                JOIN "Timesheet" timesheet ON timesheet."Year" = 2026 AND timesheet."Month" = 1
                JOIN "Employee" employee ON employee."Id" = timesheet."EmployeeId" AND employee."PersonalNumber" = '3001'
                JOIN "Attendance" attendance ON attendance."TimesheetId" = timesheet."Id"
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "AttendanceDay"
                WHERE "Id" >= UUID '80000000-0000-0000-0001-000000000001'
                  AND "Id" <= UUID '80000000-0000-0000-0001-000000000031';

                DELETE FROM "EmployeeWorkload"
                WHERE "Id" = UUID 'D0000000-0000-0000-0000-000000000001'
                  AND "EmployeeId" IN (
                      SELECT "Id"
                      FROM "Employee"
                      WHERE "PersonalNumber" = '3001'
                  );
                """);
        }
    }
}
