using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCourseTakensGlobalCourseExclusivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CourseTakens_ActiveCourse",
                table: "CourseTakens");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CourseTakens_ActiveCourse",
                table: "CourseTakens",
                column: "CourseId",
                unique: true,
                filter: "[IsActive] = 1");
        }
    }
}
