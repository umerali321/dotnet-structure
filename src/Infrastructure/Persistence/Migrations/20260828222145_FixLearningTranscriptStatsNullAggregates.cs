using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixLearningTranscriptStatsNullAggregates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SUM(CASE WHEN ... THEN 1 ELSE 0 END) returns SQL NULL, not 0, when the scoped set has
            // zero rows (e.g. a brand-new company with no imported transcript data yet) - confirmed
            // live: LearningTranscriptQueryService.GetStatsAsync's SqlQuery<StatsRow> threw "Data is
            // Null" trying to read that NULL into the non-nullable int TotalCompletions/TotalInProgress
            // properties. ISNULL(...) here is the fix; PeopleWithActivity/DistinctCoursesTaken/
            // TotalActivityRows already return 0 (not NULL) from COUNT() over zero rows, so they
            // didn't need it.
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.sp_LearningTranscriptStats
    @RestrictToCompanyIds NVARCHAR(MAX) = NULL,
    @RestrictToUserId INT = NULL,
    @RestrictToManagerId INT = NULL,
    @DateFrom DATE = NULL,
    @DateTo DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Scoped AS (
        SELECT act.LearningTranscriptActivityId, u.UserId, act.CompletionStatus, act.AssetId
        FROM dbo.LearningTranscriptActivities act
        JOIN dbo.LearningTranscriptIdentities i ON i.LearningTranscriptIdentityId = act.LearningTranscriptIdentityId AND i.UserId IS NOT NULL
        JOIN dbo.Users u ON u.UserId = i.UserId
        LEFT JOIN dbo.StudentProfiles sp ON sp.UserId = u.UserId
        WHERE act.IsLatest = 1
          AND (@RestrictToCompanyIds IS NULL OR EXISTS (
                SELECT 1 FROM dbo.UserCompanyRoles ucr2
                WHERE ucr2.UserId = u.UserId AND ucr2.IsActive = 1
                  AND ucr2.CompanyId IN (SELECT CAST(value AS INT) FROM STRING_SPLIT(@RestrictToCompanyIds, ','))
          ))
          AND (@RestrictToUserId IS NULL OR u.UserId = @RestrictToUserId)
          AND (@RestrictToManagerId IS NULL OR sp.ManagerId = @RestrictToManagerId OR sp.ManagerId IS NULL)
          AND (@DateFrom IS NULL OR COALESCE(act.LastAccessDate, act.EnrollmentDate) >= @DateFrom)
          AND (@DateTo IS NULL OR COALESCE(act.LastAccessDate, act.EnrollmentDate) <= @DateTo)
    )
    SELECT
        COUNT(DISTINCT UserId) AS PeopleWithActivity,
        COUNT(DISTINCT AssetId) AS DistinctCoursesTaken,
        ISNULL(SUM(CASE WHEN CompletionStatus = 'Completed' THEN 1 ELSE 0 END), 0) AS TotalCompletions,
        ISNULL(SUM(CASE WHEN CompletionStatus = 'In Progress' THEN 1 ELSE 0 END), 0) AS TotalInProgress,
        COUNT(*) AS TotalActivityRows,
        CASE WHEN COUNT(*) = 0 THEN 0
             ELSE CAST(SUM(CASE WHEN CompletionStatus = 'Completed' THEN 1 ELSE 0 END) AS DECIMAL(9,2)) * 100.0 / COUNT(*)
        END AS CompletionRatePercent
    FROM Scoped;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op - see FixLearningTranscriptManagerScoping's Down() for the same reasoning:
            // never reintroduce a confirmed bug on rollback.
        }
    }
}
