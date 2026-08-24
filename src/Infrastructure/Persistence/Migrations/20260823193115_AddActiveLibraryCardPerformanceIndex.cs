using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <summary>Pure-index migration, no model/shape change - same follow-up as
    /// AddStudentListPerformanceIndexes. sys.dm_db_missing_index_details flagged ActiveLibraryCards
    /// at ~90% estimated impact after that fix landed: ActiveLibraryCardLookup.GetActivePairsAsync
    /// (shared by both the Students AND Managers list queries, called once per page to check
    /// "does this person have an active Skillport card") was doing a full scan of this table with
    /// zero supporting indexes beyond its clustered PK - explains why /api/v1/managers started
    /// timing out the same way /api/v1/students had.</summary>
    public partial class AddActiveLibraryCardPerformanceIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ActiveLibraryCards_CompanyCode_Dates' AND object_id = OBJECT_ID('ActiveLibraryCards'))
    CREATE NONCLUSTERED INDEX IX_ActiveLibraryCards_CompanyCode_Dates
    ON ActiveLibraryCards (Company_Code, Start_Date, End_Date)
    INCLUDE (Email);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_ActiveLibraryCards_CompanyCode_Dates ON ActiveLibraryCards");
        }
    }
}
