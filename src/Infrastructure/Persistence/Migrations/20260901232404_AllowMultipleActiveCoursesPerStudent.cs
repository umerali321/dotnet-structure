using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Lets a student have more than one course in progress at a time.
    ///
    /// IX_CourseTakens_ActiveUser was a FILTERED UNIQUE index - the real enforcement behind "finish
    /// your current course before starting another". Removing the check in TakeCourseCommandHandler
    /// alone would only have turned a friendly message into a raw unique-constraint violation, so
    /// the index has to lose its uniqueness here.
    ///
    /// Why the rule went, in the customer's words: completion is derived from the Skillport usage
    /// report, which can lag by up to two days, so a course a student had genuinely finished still
    /// showed "In Progress" and blocked them from starting the next one. Nothing about the 30-day
    /// session was ever meant to cap how many courses run at once.
    ///
    /// The index itself is kept (non-unique): "this student's active courses" is read on every
    /// Course Library page load, so the seek is still worth having.
    ///
    /// Note on Down(): restoring uniqueness can legitimately FAIL once students have started a
    /// second course, because duplicate active rows then exist. That is correct - rolling this back
    /// is a decision about what to do with those extra enrolments, and silently deleting somebody's
    /// in-progress course to force the index through would be far worse than stopping.
    /// </summary>
    public partial class AllowMultipleActiveCoursesPerStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CourseTakens_ActiveUser",
                table: "CourseTakens");

            migrationBuilder.CreateIndex(
                name: "IX_CourseTakens_ActiveUser",
                table: "CourseTakens",
                column: "UserId",
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CourseTakens_ActiveUser",
                table: "CourseTakens");

            migrationBuilder.CreateIndex(
                name: "IX_CourseTakens_ActiveUser",
                table: "CourseTakens",
                column: "UserId",
                unique: true,
                filter: "[IsActive] = 1");
        }
    }
}
