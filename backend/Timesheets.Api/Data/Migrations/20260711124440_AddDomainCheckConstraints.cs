using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDomainCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Project"
                SET "EndDate" = "StartDate"
                WHERE "EndDate" IS NOT NULL AND "EndDate" < "StartDate";

                UPDATE "ContractEmployee"
                SET "EndDate" = "StartDate"
                WHERE "EndDate" IS NOT NULL AND "EndDate" < "StartDate";

                UPDATE "CoreEmployment"
                SET "EndDate" = "StartDate"
                WHERE "EndDate" IS NOT NULL AND "EndDate" < "StartDate";

                UPDATE "ContractEmployee" SET "Workload" = 0 WHERE "Workload" < 0;
                UPDATE "ContractEmployee" SET "Workload" = 1 WHERE "Workload" > 1;
                UPDATE "CoreEmployment" SET "Workload" = 0 WHERE "Workload" < 0;
                UPDATE "CoreEmployment" SET "Workload" = 1 WHERE "Workload" > 1;
                UPDATE "EmployeeWorkload" SET "Workload" = 0 WHERE "Workload" < 0;
                UPDATE "EmployeeWorkload" SET "Workload" = 1 WHERE "Workload" > 1;
                UPDATE "ProjectTimesheet" SET "Workload" = 0 WHERE "Workload" < 0;
                UPDATE "ProjectTimesheet" SET "Workload" = 1 WHERE "Workload" > 1;
                UPDATE "AttendanceDay" SET "Workload" = 0 WHERE "Workload" < 0;
                UPDATE "AttendanceDay" SET "Workload" = 1 WHERE "Workload" > 1;
                UPDATE "ProjectDay" SET "Workload" = 0 WHERE "Workload" < 0;
                UPDATE "ProjectDay" SET "Workload" = 1 WHERE "Workload" > 1;

                UPDATE "EmployeeWorkload" SET "Month" = 1 WHERE "Month" < 1;
                UPDATE "EmployeeWorkload" SET "Month" = 12 WHERE "Month" > 12;
                UPDATE "AttendanceTimesheet" SET "Month" = 1 WHERE "Month" < 1;
                UPDATE "AttendanceTimesheet" SET "Month" = 12 WHERE "Month" > 12;
                UPDATE "ProjectTimesheet" SET "Month" = 1 WHERE "Month" < 1;
                UPDATE "ProjectTimesheet" SET "Month" = 12 WHERE "Month" > 12;

                UPDATE "AttendanceDay" SET "HoursWithoutBreak" = 0 WHERE "HoursWithoutBreak" < 0;
                UPDATE "AttendanceDay" SET "HoursObligation" = 0 WHERE "HoursObligation" < 0;
                UPDATE "AttendanceDay" SET "CoreHours" = 0 WHERE "CoreHours" < 0;
                UPDATE "ProjectDay" SET "Hours" = 0 WHERE "Hours" < 0;
                UPDATE "ProjectDay" SET "HoursObligation" = 0 WHERE "HoursObligation" < 0;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProjectTimesheet_ValidMonth",
                table: "ProjectTimesheet",
                sql: "\"Month\" >= 1 AND \"Month\" <= 12");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProjectTimesheet_WorkloadRange",
                table: "ProjectTimesheet",
                sql: "\"Workload\" >= 0 AND \"Workload\" <= 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProjectDay_HoursAndWorkload",
                table: "ProjectDay",
                sql: "\"Hours\" >= 0\r\nAND \"Workload\" >= 0 AND \"Workload\" <= 1\r\nAND \"HoursObligation\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Project_ValidDateRange",
                table: "Project",
                sql: "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmployeeWorkload_ValidMonth",
                table: "EmployeeWorkload",
                sql: "\"Month\" >= 1 AND \"Month\" <= 12");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmployeeWorkload_WorkloadRange",
                table: "EmployeeWorkload",
                sql: "\"Workload\" >= 0 AND \"Workload\" <= 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CoreEmployment_ValidDateRange",
                table: "CoreEmployment",
                sql: "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CoreEmployment_WorkloadRange",
                table: "CoreEmployment",
                sql: "\"Workload\" >= 0 AND \"Workload\" <= 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ContractEmployee_ValidDateRange",
                table: "ContractEmployee",
                sql: "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ContractEmployee_WorkloadRange",
                table: "ContractEmployee",
                sql: "\"Workload\" >= 0 AND \"Workload\" <= 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AttendanceTimesheet_ValidMonth",
                table: "AttendanceTimesheet",
                sql: "\"Month\" >= 1 AND \"Month\" <= 12");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AttendanceDay_WorkloadAndHours",
                table: "AttendanceDay",
                sql: "\"Workload\" >= 0 AND \"Workload\" <= 1\r\nAND \"HoursWithoutBreak\" >= 0\r\nAND \"HoursObligation\" >= 0\r\nAND \"CoreHours\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ProjectTimesheet_ValidMonth",
                table: "ProjectTimesheet");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProjectTimesheet_WorkloadRange",
                table: "ProjectTimesheet");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProjectDay_HoursAndWorkload",
                table: "ProjectDay");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Project_ValidDateRange",
                table: "Project");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmployeeWorkload_ValidMonth",
                table: "EmployeeWorkload");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmployeeWorkload_WorkloadRange",
                table: "EmployeeWorkload");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CoreEmployment_ValidDateRange",
                table: "CoreEmployment");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CoreEmployment_WorkloadRange",
                table: "CoreEmployment");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ContractEmployee_ValidDateRange",
                table: "ContractEmployee");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ContractEmployee_WorkloadRange",
                table: "ContractEmployee");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AttendanceTimesheet_ValidMonth",
                table: "AttendanceTimesheet");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AttendanceDay_WorkloadAndHours",
                table: "AttendanceDay");
        }
    }
}
