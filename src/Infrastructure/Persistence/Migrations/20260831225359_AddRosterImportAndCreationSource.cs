using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRosterImportAndCreationSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreationSource",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<string>(
                name: "CreationSource",
                table: "UserCompanyRoles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Manual");

            // Both columns above are added NOT NULL with a "Manual" default, so every pre-existing
            // row would silently claim to have been created by hand - exactly the guess this feature
            // exists to eliminate. Restamp them as "Legacy": we know they predate source tracking,
            // and we do not know how they were really made.
            //
            // Batched because Users and UserCompanyRoles hold hundreds of thousands of rows; one
            // enormous UPDATE would hold locks and grow the transaction log far more than needed.
            migrationBuilder.Sql("""
                DECLARE @BatchSize INT = 50000;

                WHILE 1 = 1
                BEGIN
                    UPDATE TOP (@BatchSize) dbo.Users
                    SET CreationSource = 'Legacy'
                    WHERE CreationSource = 'Manual';

                    IF @@ROWCOUNT < @BatchSize BREAK;
                END;

                WHILE 1 = 1
                BEGIN
                    UPDATE TOP (@BatchSize) dbo.UserCompanyRoles
                    SET CreationSource = 'Legacy'
                    WHERE CreationSource = 'Manual';

                    IF @@ROWCOUNT < @BatchSize BREAK;
                END;
                """);

            migrationBuilder.CreateTable(
                name: "RosterImportBatches",
                columns: table => new
                {
                    RosterImportBatchId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ImportedByEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    CreatedCount = table.Column<int>(type: "int", nullable: false),
                    SkippedCount = table.Column<int>(type: "int", nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    EmployeesCreated = table.Column<int>(type: "int", nullable: false),
                    ManagersCreated = table.Column<int>(type: "int", nullable: false),
                    WelcomeEmailsSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WelcomeEmailsSentCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RosterImportBatches", x => x.RosterImportBatchId);
                });

            migrationBuilder.CreateTable(
                name: "RosterImportBatchRows",
                columns: table => new
                {
                    RosterImportBatchRowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RosterImportBatchId = table.Column<int>(type: "int", nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EmployeeType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    GiveManagerDashboard = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    EmployeeCreated = table.Column<bool>(type: "bit", nullable: false),
                    ManagerCreated = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RosterImportBatchRows", x => x.RosterImportBatchRowId);
                    table.ForeignKey(
                        name: "FK_RosterImportBatchRows_RosterImportBatches_RosterImportBatchId",
                        column: x => x.RosterImportBatchId,
                        principalTable: "RosterImportBatches",
                        principalColumn: "RosterImportBatchId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "PermissionId", "Category", "Description", "PermissionKey" },
                values: new object[] { 8, "Employees", "Import Employee Roster", "Students.Import" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[] { 8, (byte)5 });

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanyRoles_CreationSource_Role",
                table: "UserCompanyRoles",
                columns: new[] { "CreationSource", "RoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_RosterImportBatches_ImportedAt",
                table: "RosterImportBatches",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RosterImportBatchRows_Batch_User",
                table: "RosterImportBatchRows",
                columns: new[] { "RosterImportBatchId", "UserId" },
                filter: "[UserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RosterImportBatchRows");

            migrationBuilder.DropTable(
                name: "RosterImportBatches");

            migrationBuilder.DropIndex(
                name: "IX_UserCompanyRoles_CreationSource_Role",
                table: "UserCompanyRoles");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 8, (byte)5 });

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 8);

            migrationBuilder.DropColumn(
                name: "CreationSource",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreationSource",
                table: "UserCompanyRoles");
        }
    }
}
