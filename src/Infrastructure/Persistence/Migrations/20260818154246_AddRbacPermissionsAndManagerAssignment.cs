using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRbacPermissionsAndManagerAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE: EF's scaffold also generated a DropTable("SupportRequests") here - leftover
            // pending model change from an earlier, unrelated commit that removed the SupportRequest
            // C# entity without a matching migration. Deliberately left out of this migration: it's
            // a real table with existing data, unrelated to RBAC, and dropping it needs its own
            // explicit decision - not bundled silently into this feature's migration.
            migrationBuilder.AddColumn<int>(
                name: "ManagerId",
                table: "StudentProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemRole",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // The 5 pre-existing rows (Student/Manager/FDM/Admin/CompanyAdmin) are system roles -
            // only new roles created via the future Roles UI stay false.
            migrationBuilder.Sql("UPDATE [Roles] SET [IsSystemRole] = 1 WHERE [RoleId] IN (1, 2, 3, 4, 5);");

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermissionKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.PermissionId);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<byte>(type: "tinyint", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "PermissionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "PermissionId", "Category", "Description", "PermissionKey" },
                values: new object[,]
                {
                    { 1, "Students", "View the employees list and employee detail pages.", "Students.View" },
                    { 2, "Students", "Create new employees.", "Students.Create" },
                    { 3, "Students", "Edit an employee's profile.", "Students.Update" },
                    { 4, "Students", "Deactivate an employee.", "Students.Delete" },
                    { 5, "Students", "Reset an employee's password.", "Students.ManagePassword" },
                    { 6, "Students", "Assign an employee to a specific Manager.", "Students.AssignManager" },
                    { 7, "Students", "View an employee's login credentials.", "Students.ViewCredentials" },
                    { 11, "Managers", "View the managers list and manager detail pages.", "Managers.View" },
                    { 12, "Managers", "Create new managers.", "Managers.Create" },
                    { 13, "Managers", "Edit a manager's profile.", "Managers.Update" },
                    { 14, "Managers", "Reset a manager's password.", "Managers.ManagePassword" },
                    { 15, "Managers", "View a manager's login credentials.", "Managers.ViewCredentials" },
                    { 21, "Companies", "View the companies list.", "Companies.View" },
                    { 22, "Companies", "Create a new company (and its default admin).", "Companies.Create" },
                    { 31, "Roles", "View roles and the permission catalog.", "Roles.View" },
                    { 32, "Roles", "Create custom roles and edit their permissions.", "Roles.Manage" },
                    { 41, "Faq", "View FAQs.", "Faq.View" },
                    { 42, "Faq", "Create/edit/deactivate FAQs.", "Faq.Manage" },
                    { 51, "ContactInfo", "View support contacts.", "ContactInfo.View" },
                    { 52, "ContactInfo", "Create/edit/deactivate support contacts.", "ContactInfo.Manage" },
                    { 61, "SystemLogs", "View login activity logs.", "SystemLogs.View" },
                    { 71, "CourseLibrary", "Browse the course library.", "CourseLibrary.View" },
                    { 72, "CourseLibrary", "Publish/manage course library content.", "CourseLibrary.Publish" },
                    { 81, "Skillsoft", "Launch a Skillsoft course.", "Skillsoft.LaunchCourse" },
                    { 82, "Skillsoft", "View the Skillsoft learning transcript.", "Skillsoft.ViewTranscript" },
                    { 83, "Skillsoft", "Browse the Skillsoft catalog.", "Skillsoft.ViewCatalog" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 1, (byte)1 },
                    { 41, (byte)1 },
                    { 51, (byte)1 },
                    { 71, (byte)1 },
                    { 81, (byte)1 },
                    { 82, (byte)1 },
                    { 83, (byte)1 },
                    { 1, (byte)2 },
                    { 2, (byte)2 },
                    { 3, (byte)2 },
                    { 4, (byte)2 },
                    { 5, (byte)2 },
                    { 7, (byte)2 },
                    { 11, (byte)2 },
                    { 14, (byte)2 },
                    { 15, (byte)2 },
                    { 42, (byte)2 },
                    { 52, (byte)2 },
                    { 71, (byte)2 },
                    { 81, (byte)2 },
                    { 82, (byte)2 },
                    { 83, (byte)2 },
                    { 1, (byte)4 },
                    { 2, (byte)4 },
                    { 3, (byte)4 },
                    { 4, (byte)4 },
                    { 5, (byte)4 },
                    { 7, (byte)4 },
                    { 11, (byte)4 },
                    { 14, (byte)4 },
                    { 15, (byte)4 },
                    { 42, (byte)4 },
                    { 52, (byte)4 },
                    { 71, (byte)4 },
                    { 81, (byte)4 },
                    { 82, (byte)4 },
                    { 83, (byte)4 },
                    { 1, (byte)5 },
                    { 2, (byte)5 },
                    { 3, (byte)5 },
                    { 4, (byte)5 },
                    { 5, (byte)5 },
                    { 6, (byte)5 },
                    { 7, (byte)5 },
                    { 11, (byte)5 },
                    { 12, (byte)5 },
                    { 13, (byte)5 },
                    { 14, (byte)5 },
                    { 15, (byte)5 },
                    { 31, (byte)5 },
                    { 42, (byte)5 },
                    { 52, (byte)5 },
                    { 71, (byte)5 },
                    { 72, (byte)5 },
                    { 81, (byte)5 },
                    { 82, (byte)5 },
                    { 83, (byte)5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentProfiles_ManagerId",
                table: "StudentProfiles",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_PermissionKey",
                table: "Permissions",
                column: "PermissionKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_StudentProfiles_ManagerId",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "ManagerId",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "IsSystemRole",
                table: "Roles");
        }
    }
}
