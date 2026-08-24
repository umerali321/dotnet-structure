using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <summary>Pure-index migration, no model/shape change - added after ListStudents (GET
    /// /api/v1/students) started timing out under the Assign Training wizard's larger page-size
    /// call. sys.dm_db_missing_index_details confirmed these exact three indexes against real
    /// production query telemetry (up to 55.9% estimated impact on UserCompanyRoles alone) before
    /// this was written - not a guess. Raw SQL (matching sp_ListCompanies's precedent) rather than
    /// EF HasIndex(), since this DB's migration history already has unrelated model/DB drift (see
    /// AddSkillTraxAndAssignmentsTables) and a plain CREATE INDEX carries zero risk of EF trying to
    /// "reconcile" anything else.</summary>
    public partial class AddStudentListPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserCompanyRoles_RoleId_IsActive_Dates' AND object_id = OBJECT_ID('UserCompanyRoles'))
    CREATE NONCLUSTERED INDEX IX_UserCompanyRoles_RoleId_IsActive_Dates
    ON UserCompanyRoles (RoleId, IsActive, StartDate, EndDate)
    INCLUDE (UserId, CompanyId);");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_IsActive' AND object_id = OBJECT_ID('Users'))
    CREATE NONCLUSTERED INDEX IX_Users_IsActive
    ON Users (IsActive);");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StudentProfiles_StudentType' AND object_id = OBJECT_ID('StudentProfiles'))
    CREATE NONCLUSTERED INDEX IX_StudentProfiles_StudentType
    ON StudentProfiles (StudentType)
    INCLUDE (UserId);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_UserCompanyRoles_RoleId_IsActive_Dates ON UserCompanyRoles");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Users_IsActive ON Users");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_StudentProfiles_StudentType ON StudentProfiles");
        }
    }
}
