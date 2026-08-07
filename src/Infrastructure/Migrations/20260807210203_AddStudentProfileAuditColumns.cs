using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentProfileAuditColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // StudentProfiles already exists (StudentProfileId, UserId, StudentType, CreatedAt) -
            // only these three new, nullable, additive columns are added. Existing rows get NULL,
            // which the application treats as "Legacy Record".
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "StudentProfiles",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "StudentProfiles",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "StudentProfiles",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "StudentProfiles");
        }
    }
}
