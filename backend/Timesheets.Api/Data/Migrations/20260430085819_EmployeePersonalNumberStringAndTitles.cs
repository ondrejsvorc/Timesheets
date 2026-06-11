using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class EmployeePersonalNumberStringAndTitles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Postgres needs an explicit cast when changing int -> varchar.
            // Also, older databases may contain NULL personal numbers.
            migrationBuilder.Sql("""
                UPDATE "Employee"
                SET "PersonalNumber" = 0
                WHERE "PersonalNumber" IS NULL;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Employee"
                ALTER COLUMN "PersonalNumber" TYPE character varying(50)
                USING "PersonalNumber"::text;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Employee"
                ALTER COLUMN "PersonalNumber" SET NOT NULL;
                """);

            migrationBuilder.AddColumn<string>(
                name: "TitleAfter",
                table: "Employee",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleBefore",
                table: "Employee",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TitleAfter",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "TitleBefore",
                table: "Employee");

            migrationBuilder.Sql("""
                ALTER TABLE "Employee"
                ALTER COLUMN "PersonalNumber" TYPE integer
                USING NULLIF("PersonalNumber", '')::integer;
                """);
        }
    }
}
