using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDateRangeToSkillportScraperSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "CustomDateFrom",
                table: "SkillportScraperSettings",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CustomDateTo",
                table: "SkillportScraperSettings",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DateRangeMode",
                table: "SkillportScraperSettings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Today");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomDateFrom",
                table: "SkillportScraperSettings");

            migrationBuilder.DropColumn(
                name: "CustomDateTo",
                table: "SkillportScraperSettings");

            migrationBuilder.DropColumn(
                name: "DateRangeMode",
                table: "SkillportScraperSettings");
        }
    }
}
