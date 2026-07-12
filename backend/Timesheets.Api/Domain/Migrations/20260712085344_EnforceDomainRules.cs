using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timesheets.Api.Domain.Migrations
{
    /// <inheritdoc />
    public partial class EnforceDomainRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContractEmployee_Employee_EmployeeId",
                table: "ContractEmployee");

            migrationBuilder.DropForeignKey(
                name: "FK_ContractManager_Employee_EmployeeId",
                table: "ContractManager");

            migrationBuilder.DropForeignKey(
                name: "FK_ContractPart_ContractEmployee_ContractEmployeeId",
                table: "ContractPart");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectManager_Employee_EmployeeId",
                table: "ProjectManager");

            migrationBuilder.DropIndex(
                name: "IX_Project_Name",
                table: "Project");

            migrationBuilder.DropIndex(
                name: "IX_Project_RegistrationNumber",
                table: "Project");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmployeeWorkload_WorkloadRange",
                table: "EmployeeWorkload");

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

            migrationBuilder.Sql(
                """
                UPDATE "Project"
                SET "NormalizedName" = lower(regexp_replace(btrim("Name"), '\s+', ' ', 'g')),
                    "NormalizedRegistrationNumber" = lower(regexp_replace("RegistrationNumber", '\s+', '', 'g'));

                UPDATE "Contract"
                SET "NormalizedName" = lower(regexp_replace(btrim("Name"), '\s+', ' ', 'g')),
                    "NormalizedRegistrationNumber" = lower(regexp_replace("RegistrationNumber", '\s+', '', 'g'));
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_TimesheetStatusHistory_DistinctStatuses",
                table: "TimesheetStatusHistory",
                sql: "\"FromStatusId\" IS NULL OR \"FromStatusId\" <> \"ToStatusId\"");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetStatus_Name",
                table: "TimesheetStatus",
                column: "Name",
                unique: true);

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
                name: "IX_Interruption_Name",
                table: "Interruption",
                column: "Name",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmployeeWorkload_WorkloadRange",
                table: "EmployeeWorkload",
                sql: "\"Workload\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeType_Name",
                table: "EmployeeType",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employee_PersonalNumber",
                table: "Employee",
                column: "PersonalNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractPart_LockedBy",
                table: "ContractPart",
                column: "LockedBy");

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

            migrationBuilder.AddForeignKey(
                name: "FK_ContractEmployee_Employee_EmployeeId",
                table: "ContractEmployee",
                column: "EmployeeId",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContractManager_Employee_EmployeeId",
                table: "ContractManager",
                column: "EmployeeId",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContractPart_ContractEmployee_ContractEmployeeId",
                table: "ContractPart",
                column: "ContractEmployeeId",
                principalTable: "ContractEmployee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContractPart_Employee_LockedBy",
                table: "ContractPart",
                column: "LockedBy",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectManager_Employee_EmployeeId",
                table: "ProjectManager",
                column: "EmployeeId",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContractEmployee_Employee_EmployeeId",
                table: "ContractEmployee");

            migrationBuilder.DropForeignKey(
                name: "FK_ContractManager_Employee_EmployeeId",
                table: "ContractManager");

            migrationBuilder.DropForeignKey(
                name: "FK_ContractPart_ContractEmployee_ContractEmployeeId",
                table: "ContractPart");

            migrationBuilder.DropForeignKey(
                name: "FK_ContractPart_Employee_LockedBy",
                table: "ContractPart");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectManager_Employee_EmployeeId",
                table: "ProjectManager");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TimesheetStatusHistory_DistinctStatuses",
                table: "TimesheetStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_TimesheetStatus_Name",
                table: "TimesheetStatus");

            migrationBuilder.DropIndex(
                name: "IX_Project_NormalizedName",
                table: "Project");

            migrationBuilder.DropIndex(
                name: "IX_Project_NormalizedRegistrationNumber",
                table: "Project");

            migrationBuilder.DropIndex(
                name: "IX_Interruption_Name",
                table: "Interruption");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmployeeWorkload_WorkloadRange",
                table: "EmployeeWorkload");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeType_Name",
                table: "EmployeeType");

            migrationBuilder.DropIndex(
                name: "IX_Employee_PersonalNumber",
                table: "Employee");

            migrationBuilder.DropIndex(
                name: "IX_ContractPart_LockedBy",
                table: "ContractPart");

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

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmployeeWorkload_WorkloadRange",
                table: "EmployeeWorkload",
                sql: "\"Workload\" >= 0 AND \"Workload\" <= 1");

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

            migrationBuilder.AddForeignKey(
                name: "FK_ContractEmployee_Employee_EmployeeId",
                table: "ContractEmployee",
                column: "EmployeeId",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ContractManager_Employee_EmployeeId",
                table: "ContractManager",
                column: "EmployeeId",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ContractPart_ContractEmployee_ContractEmployeeId",
                table: "ContractPart",
                column: "ContractEmployeeId",
                principalTable: "ContractEmployee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectManager_Employee_EmployeeId",
                table: "ProjectManager",
                column: "EmployeeId",
                principalTable: "Employee",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
