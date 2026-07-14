using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Domain.Migrations
{
    /// <inheritdoc />
    public partial class ContractEmployeeAssignmentIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContractEmployee_ContractId_EmployeeId_Position",
                table: "ContractEmployee");

            migrationBuilder.DropIndex(
                name: "IX_ContractEmployee_EmployeeId",
                table: "ContractEmployee");

            migrationBuilder.CreateIndex(
                name: "IX_ContractEmployee_ContractId_EmployeeId_PositionCode_StartDa~",
                table: "ContractEmployee",
                columns: new[] { "ContractId", "EmployeeId", "PositionCode", "StartDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractEmployee_EmployeeId_StartDate_EndDate",
                table: "ContractEmployee",
                columns: new[] { "EmployeeId", "StartDate", "EndDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContractEmployee_ContractId_EmployeeId_PositionCode_StartDa~",
                table: "ContractEmployee");

            migrationBuilder.DropIndex(
                name: "IX_ContractEmployee_EmployeeId_StartDate_EndDate",
                table: "ContractEmployee");

            migrationBuilder.CreateIndex(
                name: "IX_ContractEmployee_ContractId_EmployeeId_Position",
                table: "ContractEmployee",
                columns: new[] { "ContractId", "EmployeeId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractEmployee_EmployeeId",
                table: "ContractEmployee",
                column: "EmployeeId");
        }
    }
}
