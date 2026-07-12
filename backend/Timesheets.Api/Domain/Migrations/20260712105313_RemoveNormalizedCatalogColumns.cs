using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Domain.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNormalizedCatalogColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Project_NormalizedName",
                table: "Project");

            migrationBuilder.DropIndex(
                name: "IX_Project_NormalizedRegistrationNumber",
                table: "Project");

            migrationBuilder.DropIndex(
                name: "IX_Contract_ProjectId_NormalizedName",
                table: "Contract");

            migrationBuilder.DropIndex(
                name: "IX_Contract_ProjectId_NormalizedRegistrationNumber",
                table: "Contract");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "NormalizedRegistrationNumber",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Contract");

            migrationBuilder.DropColumn(
                name: "NormalizedRegistrationNumber",
                table: "Contract");

            migrationBuilder.CreateIndex(
                name: "IX_Project_Name",
                table: "Project",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Project_RegistrationNumber",
                table: "Project",
                column: "RegistrationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contract_ProjectId_Name",
                table: "Contract",
                columns: new[] { "ProjectId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contract_ProjectId_RegistrationNumber",
                table: "Contract",
                columns: new[] { "ProjectId", "RegistrationNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Project_Name",
                table: "Project");

            migrationBuilder.DropIndex(
                name: "IX_Project_RegistrationNumber",
                table: "Project");

            migrationBuilder.DropIndex(
                name: "IX_Contract_ProjectId_Name",
                table: "Contract");

            migrationBuilder.DropIndex(
                name: "IX_Contract_ProjectId_RegistrationNumber",
                table: "Contract");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Project",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedRegistrationNumber",
                table: "Project",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Contract",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedRegistrationNumber",
                table: "Contract",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "Project"
                SET "NormalizedName" = lower(regexp_replace(btrim("Name"), '\s+', ' ', 'g')),
                    "NormalizedRegistrationNumber" = lower(regexp_replace("RegistrationNumber", '\s+', '', 'g'));
                """);

            migrationBuilder.Sql("""
                UPDATE "Contract"
                SET "NormalizedName" = lower(regexp_replace(btrim("Name"), '\s+', ' ', 'g')),
                    "NormalizedRegistrationNumber" = lower(regexp_replace("RegistrationNumber", '\s+', '', 'g'));
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Project_NormalizedName",
                table: "Project",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Project_NormalizedRegistrationNumber",
                table: "Project",
                column: "NormalizedRegistrationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contract_ProjectId_NormalizedName",
                table: "Contract",
                columns: new[] { "ProjectId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contract_ProjectId_NormalizedRegistrationNumber",
                table: "Contract",
                columns: new[] { "ProjectId", "NormalizedRegistrationNumber" },
                unique: true);
        }
    }
}
