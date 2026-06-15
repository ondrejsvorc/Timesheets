using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnforceNormalizedContractUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Contract_ProjectId_Name",
                table: "Contract");

            migrationBuilder.DropIndex(
                name: "IX_Contract_ProjectId_RegistrationNumber",
                table: "Contract");

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_Contract_ProjectId_Name"
                ON "Contract" ("ProjectId", lower(btrim("Name")));
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_Contract_ProjectId_RegistrationNumber"
                ON "Contract" ("ProjectId", lower(btrim("RegistrationNumber")));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Contract_ProjectId_Name",
                table: "Contract");

            migrationBuilder.DropIndex(
                name: "IX_Contract_ProjectId_RegistrationNumber",
                table: "Contract");

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
    }
}
