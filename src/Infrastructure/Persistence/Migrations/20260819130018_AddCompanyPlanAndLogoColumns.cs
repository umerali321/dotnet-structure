using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyPlanAndLogoColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Companies",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PlanEndDate",
                table: "Companies",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(2099, 12, 31));

            migrationBuilder.AddColumn<DateOnly>(
                name: "PlanStartDate",
                table: "Companies",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(2020, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "PlanType",
                table: "Companies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "License");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "PlanEndDate",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "PlanStartDate",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "PlanType",
                table: "Companies");
        }
    }
}
