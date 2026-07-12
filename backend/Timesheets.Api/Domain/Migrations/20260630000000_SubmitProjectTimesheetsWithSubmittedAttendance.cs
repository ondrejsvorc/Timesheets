using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Domain.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260630000000_SubmitProjectTimesheetsWithSubmittedAttendance")]
    public partial class SubmitProjectTimesheetsWithSubmittedAttendance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "ProjectTimesheet" AS project
                SET "TimesheetStatusId" = '00000000-0000-0000-0000-000000000021',
                    "UpdatedAt" = NOW()
                FROM "AttendanceTimesheet" AS attendance
                WHERE attendance."EmployeeId" = project."EmployeeId"
                  AND attendance."Year" = project."Year"
                  AND attendance."Month" = project."Month"
                  AND attendance."TimesheetStatusId" = '00000000-0000-0000-0000-000000000021'
                  AND project."TimesheetStatusId" = '00000000-0000-0000-0000-000000000020';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
