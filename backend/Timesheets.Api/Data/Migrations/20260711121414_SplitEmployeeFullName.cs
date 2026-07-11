using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SplitEmployeeFullName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Employee",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Surname",
                table: "Employee",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Employee"
                SET
                    "Surname" = COALESCE((regexp_match(trim("FullName"), '(\S+)$'))[1], trim("FullName"), 'Unknown'),
                    "FirstName" = COALESCE(NULLIF(trim(regexp_replace(trim("FullName"), '\s+\S+$', '')), ''), COALESCE((regexp_match(trim("FullName"), '(\S+)$'))[1], trim("FullName"), 'Unknown'))
                WHERE "FullName" IS NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Employee"
                SET
                    "FirstName" = COALESCE("FirstName", 'Unknown'),
                    "Surname" = COALESCE("Surname", 'Unknown')
                WHERE "FirstName" IS NULL OR "Surname" IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Employee");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Employee",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Surname",
                table: "Employee",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employee_Surname",
                table: "Employee",
                column: "Surname");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_Surname_FirstName",
                table: "Employee",
                columns: new[] { "Surname", "FirstName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employee_Surname",
                table: "Employee");

            migrationBuilder.DropIndex(
                name: "IX_Employee_Surname_FirstName",
                table: "Employee");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Employee",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE "Employee"
                SET "FullName" = trim("FirstName" || ' ' || "Surname");
                """);

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "Surname",
                table: "Employee");
        }
    }
}
