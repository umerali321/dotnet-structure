using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillTraxAndAssignmentUpdatedByTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "SkillTrax",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "SkillTrax",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Assignments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "Assignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillTrax_UpdatedByUserId",
                table: "SkillTrax",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_UpdatedByUserId",
                table: "Assignments",
                column: "UpdatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Users_UpdatedByUserId",
                table: "Assignments",
                column: "UpdatedByUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SkillTrax_Users_UpdatedByUserId",
                table: "SkillTrax",
                column: "UpdatedByUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Users_UpdatedByUserId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_SkillTrax_Users_UpdatedByUserId",
                table: "SkillTrax");

            migrationBuilder.DropIndex(
                name: "IX_SkillTrax_UpdatedByUserId",
                table: "SkillTrax");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_UpdatedByUserId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SkillTrax");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "SkillTrax");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Assignments");
        }
    }
}
