using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectAndContractCatalogUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Contract_ProjectId",
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

            migrationBuilder.CreateIndex(
                name: "IX_Contract_ProjectId",
                table: "Contract",
                column: "ProjectId");
        }
    }
}
