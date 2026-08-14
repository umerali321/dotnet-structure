using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillportSessionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ActiveLibraryCards is an existing legacy production table (mapped via HasNoKey() in
            // ActiveLibraryCardConfiguration) that was never given its own migration - the CreateTable
            // EF generated for it here was removed by hand. This migration only ever touches the
            // brand-new SkillportSessions table.
            migrationBuilder.CreateTable(
                name: "SkillportSessions",
                columns: table => new
                {
                    SkillportSessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    SkillportUsername = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SkillportPassword = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillportSessions", x => x.SkillportSessionId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SkillportSessions_UserId_CompanyId",
                table: "SkillportSessions",
                columns: new[] { "UserId", "CompanyId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SkillportSessions");
        }
    }
}
