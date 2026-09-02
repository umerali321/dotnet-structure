using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSettingsPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "PermissionId", "Category", "Description", "PermissionKey" },
                values: new object[,]
                {
                    { 121, "Settings", "Access Settings", "Settings.View" },
                    { 122, "Settings", "Manage Email Settings", "Settings.ManageEmail" },
                    { 123, "Settings", "View Email History", "Settings.ViewEmailHistory" },
                    { 124, "Settings", "Manage Notification Service", "Settings.ManageNotifications" },
                    { 125, "Settings", "Manage Report Scraper", "Settings.ManageScraper" },
                    { 126, "Settings", "Manage App Settings", "Settings.ManageAppSettings" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 121);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 122);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 123);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 124);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 125);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 126);
        }
    }
}
