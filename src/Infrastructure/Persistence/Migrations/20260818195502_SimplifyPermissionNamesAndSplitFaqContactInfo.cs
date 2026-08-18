using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyPermissionNamesAndSplitFaqContactInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Employees", "View Employees" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Employees", "Create Employees" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Employees", "Edit Employees" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Employees", "Delete Employees" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 5,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Employees", "Reset Employee Password" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Employees", "Assign Employee to a Manager" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Employees", "View Employee Login Credentials" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 11,
                column: "Description",
                value: "View Managers");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 12,
                column: "Description",
                value: "Create Managers");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 13,
                column: "Description",
                value: "Edit Managers");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 14,
                column: "Description",
                value: "Reset Manager Password");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 15,
                column: "Description",
                value: "View Manager Login Credentials");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 21,
                column: "Description",
                value: "View Companies");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 22,
                column: "Description",
                value: "Create Companies");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 31,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Roles & Permissions", "View Roles & Permissions" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 32,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Roles & Permissions", "Create & Edit Roles" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 41,
                columns: new[] { "Category", "Description" },
                values: new object[] { "FAQs", "View FAQs" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 42,
                columns: new[] { "Category", "Description", "PermissionKey" },
                values: new object[] { "FAQs", "Create FAQs", "Faq.Create" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 51,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Contact Info", "View Contact Info" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 52,
                columns: new[] { "Category", "Description", "PermissionKey" },
                values: new object[] { "Contact Info", "Create Contact Info", "ContactInfo.Create" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 61,
                columns: new[] { "Category", "Description" },
                values: new object[] { "System Logs", "View System Logs" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 71,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Course Library", "View Course Library" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 72,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Course Library", "Publish Courses" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 81,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Course Provider", "Launch Courses" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 82,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Course Provider", "View Learning Transcript" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 83,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Course Provider", "View Course Catalog" });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "PermissionId", "Category", "Description", "PermissionKey" },
                values: new object[,]
                {
                    { 43, "FAQs", "Edit FAQs", "Faq.Update" },
                    { 44, "FAQs", "Delete FAQs", "Faq.Delete" },
                    { 53, "Contact Info", "Edit Contact Info", "ContactInfo.Update" },
                    { 54, "Contact Info", "Delete Contact Info", "ContactInfo.Delete" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 41, (byte)2 },
                    { 51, (byte)2 },
                    { 41, (byte)4 },
                    { 51, (byte)4 },
                    { 41, (byte)5 },
                    { 51, (byte)5 },
                    { 43, (byte)2 },
                    { 44, (byte)2 },
                    { 53, (byte)2 },
                    { 54, (byte)2 },
                    { 43, (byte)4 },
                    { 44, (byte)4 },
                    { 53, (byte)4 },
                    { 54, (byte)4 },
                    { 43, (byte)5 },
                    { 44, (byte)5 },
                    { 53, (byte)5 },
                    { 54, (byte)5 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 41, (byte)2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 43, (byte)2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 44, (byte)2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 51, (byte)2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 53, (byte)2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 54, (byte)2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 41, (byte)4 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 43, (byte)4 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 44, (byte)4 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 51, (byte)4 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 53, (byte)4 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 54, (byte)4 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 41, (byte)5 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 43, (byte)5 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 44, (byte)5 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 51, (byte)5 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 53, (byte)5 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 54, (byte)5 });

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 54);

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Students", "View the employees list and employee detail pages." });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Students", "Create new employees." });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Students", "Edit an employee's profile." });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Students", "Deactivate an employee." });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 5,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Students", "Reset an employee's password." });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Students", "Assign an employee to a specific Manager." });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Students", "View an employee's login credentials." });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 11,
                column: "Description",
                value: "View the managers list and manager detail pages.");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 12,
                column: "Description",
                value: "Create new managers.");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 13,
                column: "Description",
                value: "Edit a manager's profile.");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 14,
                column: "Description",
                value: "Reset a manager's password.");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 15,
                column: "Description",
                value: "View a manager's login credentials.");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 21,
                column: "Description",
                value: "View the companies list.");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 22,
                column: "Description",
                value: "Create a new company (and its default admin).");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 31,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Roles", "View roles and the permission catalog." });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 32,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Roles", "Create custom roles and edit their permissions." });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 41,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Faq", "View FAQs." });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 42,
                columns: new[] { "Category", "Description", "PermissionKey" },
                values: new object[] { "Faq", "Create/edit/deactivate FAQs.", "Faq.Manage" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 51,
                columns: new[] { "Category", "Description" },
                values: new object[] { "ContactInfo", "View support contacts." });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 52,
                columns: new[] { "Category", "Description", "PermissionKey" },
                values: new object[] { "ContactInfo", "Create/edit/deactivate support contacts.", "ContactInfo.Manage" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 61,
                columns: new[] { "Category", "Description" },
                values: new object[] { "SystemLogs", "View login activity logs." });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 71,
                columns: new[] { "Category", "Description" },
                values: new object[] { "CourseLibrary", "Browse the course library." });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 72,
                columns: new[] { "Category", "Description" },
                values: new object[] { "CourseLibrary", "Publish/manage course library content." });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 81,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Skillsoft", "Launch a Skillsoft course." });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 82,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Skillsoft", "View the Skillsoft learning transcript." });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 83,
                columns: new[] { "Category", "Description" },
                values: new object[] { "Skillsoft", "Browse the Skillsoft catalog." });
        }
    }
}
