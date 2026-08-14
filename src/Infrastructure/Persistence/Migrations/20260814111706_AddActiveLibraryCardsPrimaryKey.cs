using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveLibraryCardsPrimaryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActiveLibraryCardId",
                table: "ActiveLibraryCards",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ActiveLibraryCards",
                table: "ActiveLibraryCards",
                column: "ActiveLibraryCardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ActiveLibraryCards",
                table: "ActiveLibraryCards");

            migrationBuilder.DropColumn(
                name: "ActiveLibraryCardId",
                table: "ActiveLibraryCards");
        }
    }
}
