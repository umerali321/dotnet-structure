using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <summary>Pure-index migration, no model/shape change - same follow-up as
    /// AddStudentListPerformanceIndexes/AddCompanyStatusPerformanceIndex/
    /// AddStudentProfilesManagerIdCoveringIndex. GET /api/v1/students?search=... timed out (30s SQL
    /// command timeout) for an unscoped (SuperAdmin, no companyId) free-text search. Root cause,
    /// confirmed via SET STATISTICS IO/TIME on the reconstructed query and a before/after test index:
    /// StudentQueryService's "studentMemberships" filter is a correlated EXISTS against
    /// UserCompanyRoles JOIN Companies JOIN Roles, re-evaluated once per outer StudentProfiles/Users
    /// row (Scan count 162068 on UserCompanyRoles, ~1,000,000 logical reads). Every existing
    /// UserCompanyRoles index leads with RoleId or is UserId-only without covering IsActive/dates -
    /// none let SQL Server seek "does this UserId have an active Student role" in one shot, so it
    /// fell back to scanning the RoleId-led index and filtering by UserId as a residual predicate,
    /// 162,068 times over. This index leads with UserId (the correlated column) and covers every
    /// other column the EXISTS clause touches, so each of those 162,068 lookups becomes a single
    /// covering seek instead of a scan - cut the reconstructed query from ~40s to ~4.4s in isolated
    /// testing (COUNT + data-fetch together, both same shape, ~13.5s end-to-end via the API, down
    /// from a 30s timeout). Verified no regression on company-scoped calls (Manager/CompanyAdmin's
    /// normal case), which were already sub-second and remain so.</summary>
    public partial class AddUserCompanyRolesUserIdCoveringIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserCompanyRoles_UserId_IsActive_Covering' AND object_id = OBJECT_ID('UserCompanyRoles'))
    CREATE NONCLUSTERED INDEX IX_UserCompanyRoles_UserId_IsActive_Covering
    ON UserCompanyRoles (UserId, IsActive)
    INCLUDE (RoleId, CompanyId, StartDate, EndDate);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_UserCompanyRoles_UserId_IsActive_Covering ON UserCompanyRoles");
        }
    }
}
