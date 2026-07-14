using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Domain.Migrations
{
    /// <inheritdoc />
    public partial class ProjectTimesheetUniqueByContractAndAutoTimesheetsOnAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectTimesheet_EmployeeId_Year_Month",
                table: "ProjectTimesheet");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTimesheet_EmployeeId_ContractId_Year_Month",
                table: "ProjectTimesheet",
                columns: new[] { "EmployeeId", "ContractId", "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectTimesheet_EmployeeId_ContractId_Year_Month",
                table: "ProjectTimesheet");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTimesheet_EmployeeId_Year_Month",
                table: "ProjectTimesheet",
                columns: new[] { "EmployeeId", "Year", "Month" },
                unique: true);
        }
    }
}
