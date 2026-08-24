using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <summary>Pure-index migration, no model/shape change - same follow-up as
    /// AddStudentListPerformanceIndexes/AddActiveLibraryCardPerformanceIndex/
    /// AddCompanyStatusPerformanceIndex. sys.dm_db_missing_index_details flagged StudentProfiles
    /// (ManagerId) INCLUDE (UserId) as the top-impact missing index (162K+ rows) when
    /// /api/v1/students timed out again for a plain Manager caller - StudentQueryService's
    /// RestrictToManagerId branch filters on ManagerId, and the existing plain
    /// IX_StudentProfiles_ManagerId index (key-only) forced a bookmark lookup back to the
    /// clustered index for UserId on every matching row. Replaced with a covering version instead
    /// of keeping both, since any query that used the old index can use this one too.</summary>
    public partial class AddStudentProfilesManagerIdCoveringIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StudentProfiles_ManagerId' AND object_id = OBJECT_ID('StudentProfiles'))
    DROP INDEX IX_StudentProfiles_ManagerId ON StudentProfiles;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StudentProfiles_ManagerId' AND object_id = OBJECT_ID('StudentProfiles'))
    CREATE NONCLUSTERED INDEX IX_StudentProfiles_ManagerId
    ON StudentProfiles (ManagerId)
    INCLUDE (UserId);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StudentProfiles_ManagerId' AND object_id = OBJECT_ID('StudentProfiles'))
    DROP INDEX IX_StudentProfiles_ManagerId ON StudentProfiles;

CREATE NONCLUSTERED INDEX IX_StudentProfiles_ManagerId
ON StudentProfiles (ManagerId);");
        }
    }
}
