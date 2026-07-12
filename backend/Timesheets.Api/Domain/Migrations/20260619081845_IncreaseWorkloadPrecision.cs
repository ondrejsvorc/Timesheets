using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Domain.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseWorkloadPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Workload",
                table: "ProjectTimesheet",
                type: "numeric(7,4)",
                precision: 7,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Workload",
                table: "ProjectDay",
                type: "numeric(7,4)",
                precision: 7,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Workload",
                table: "EmployeeWorkload",
                type: "numeric(7,4)",
                precision: 7,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Workload",
                table: "CoreEmployment",
                type: "numeric(7,4)",
                precision: 7,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Workload",
                table: "ContractEmployee",
                type: "numeric(7,4)",
                precision: 7,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Workload",
                table: "AttendanceDay",
                type: "numeric(7,4)",
                precision: 7,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Workload",
                table: "ProjectTimesheet",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(7,4)",
                oldPrecision: 7,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "Workload",
                table: "ProjectDay",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(7,4)",
                oldPrecision: 7,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "Workload",
                table: "EmployeeWorkload",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(7,4)",
                oldPrecision: 7,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "Workload",
                table: "CoreEmployment",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(7,4)",
                oldPrecision: 7,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "Workload",
                table: "ContractEmployee",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(7,4)",
                oldPrecision: 7,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "Workload",
                table: "AttendanceDay",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(7,4)",
                oldPrecision: 7,
                oldScale: 4);
        }
    }
}
