using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <summary>Pure-index migration, no model/shape change - same follow-up as
    /// AddStudentListPerformanceIndexes/AddActiveLibraryCardPerformanceIndex.
    /// sys.dm_db_missing_index_details flagged Companies (IsActive, PlanType) at ~81% impact,
    /// used by the company list/plan-expiry checks (e.g. sp_ListCompanies, CompanyContextResolver
    /// paths) with no supporting index beyond the clustered PK.</summary>
    public partial class AddCompanyStatusPerformanceIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Companies_IsActive_PlanType' AND object_id = OBJECT_ID('Companies'))
    CREATE NONCLUSTERED INDEX IX_Companies_IsActive_PlanType
    ON Companies (IsActive, PlanType)
    INCLUDE (PlanEndDate);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Companies_IsActive_PlanType ON Companies");
        }
    }
}
