using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixLearningTranscriptManagerScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A plain Manager (@RestrictToManagerId set) must see employees explicitly assigned to
            // them OR still-unassigned employees in a company they manage - mirroring
            // StudentAuthorization's existing rule everywhere else in the app. The original version
            // of this condition (sp.ManagerId = @RestrictToManagerId only) wrongly hid every
            // unassigned employee from every Manager.
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.sp_ListLearningTranscript
    @RestrictToCompanyIds NVARCHAR(MAX) = NULL, -- comma-separated CompanyIds, NULL = no restriction
    @RestrictToUserId INT = NULL,               -- Employee self-view: forces exactly this user
    @RestrictToManagerId INT = NULL,            -- Manager view: narrows to their assigned + unassigned employees
    @Search NVARCHAR(200) = NULL,
    @AssetId NVARCHAR(200) = NULL,
    @CompletionStatus NVARCHAR(50) = NULL,
    @DateFrom DATE = NULL,
    @DateTo DATE = NULL,
    @Page INT = 1,
    @PageSize INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Term NVARCHAR(204) = NULL;
    IF @Search IS NOT NULL AND LTRIM(RTRIM(@Search)) <> ''
        SET @Term = '%' + REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(@Search)), '\', '\\'), '%', '\%'), '_', '\_') + '%';

    SELECT
        act.LearningTranscriptActivityId,
        u.UserId, u.FirstName AS EmployeeFirstName, u.LastName AS EmployeeLastName, u.Email AS EmployeeEmail,
        comp.CompanyId, comp.CompanyName,
        sp.ManagerId, mgr.FirstName AS ManagerFirstName, mgr.LastName AS ManagerLastName,
        a.AssetId, a.AssetTitle, a.AssetType, a.AssetSubType,
        act.EnrollmentDate, act.FirstAccessDate, act.LastAccessDate, act.CompletionDate, act.CompletionStatus,
        act.HighScore, act.CurrentScore, act.PreTestScore,
        act.MaxTestAttempts, act.ActualTestAttempts,
        act.ExpectedDurationMinutes, act.ActualDurationMinutes,
        act.TimesAccessed, act.TimesDownloaded, act.TimesRestarted,
        act.AbsoluteFirstAccessDate, act.AbsoluteLastAccessDate, act.AbsoluteTimesAccessed,
        act.AbsoluteHighScore, act.AbsoluteLastScore, act.AbsoluteActualDurationMinutes,
        act.LastSkillportLoginDate, act.SkillportRegistrationDate,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.LearningTranscriptActivities act
    JOIN dbo.LearningTranscriptIdentities i ON i.LearningTranscriptIdentityId = act.LearningTranscriptIdentityId AND i.UserId IS NOT NULL
    JOIN dbo.Users u ON u.UserId = i.UserId
    JOIN dbo.LearningTranscriptAssets a ON a.AssetId = act.AssetId
    LEFT JOIN dbo.StudentProfiles sp ON sp.UserId = u.UserId
    LEFT JOIN dbo.Users mgr ON mgr.UserId = sp.ManagerId
    OUTER APPLY (
        SELECT TOP 1 c.CompanyId, c.CompanyName
        FROM dbo.UserCompanyRoles ucr
        JOIN dbo.Companies c ON c.CompanyId = ucr.CompanyId
        WHERE ucr.UserId = u.UserId AND ucr.IsActive = 1
        ORDER BY ucr.StartDate DESC
    ) comp
    WHERE act.IsLatest = 1
      AND (@RestrictToCompanyIds IS NULL OR EXISTS (
            SELECT 1 FROM dbo.UserCompanyRoles ucr2
            WHERE ucr2.UserId = u.UserId AND ucr2.IsActive = 1
              AND ucr2.CompanyId IN (SELECT CAST(value AS INT) FROM STRING_SPLIT(@RestrictToCompanyIds, ','))
      ))
      AND (@RestrictToUserId IS NULL OR u.UserId = @RestrictToUserId)
      AND (@RestrictToManagerId IS NULL OR sp.ManagerId = @RestrictToManagerId OR sp.ManagerId IS NULL)
      AND (@AssetId IS NULL OR a.AssetId = @AssetId)
      AND (@CompletionStatus IS NULL OR act.CompletionStatus = @CompletionStatus)
      AND (@DateFrom IS NULL OR COALESCE(act.LastAccessDate, act.EnrollmentDate) >= @DateFrom)
      AND (@DateTo IS NULL OR COALESCE(act.LastAccessDate, act.EnrollmentDate) <= @DateTo)
      AND (@Term IS NULL
           OR u.FirstName LIKE @Term ESCAPE '\' OR u.LastName LIKE @Term ESCAPE '\'
           OR u.Email LIKE @Term ESCAPE '\' OR a.AssetTitle LIKE @Term ESCAPE '\')
    ORDER BY u.LastName, u.FirstName, a.AssetTitle
    OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END");

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
        SUM(CASE WHEN CompletionStatus = 'Completed' THEN 1 ELSE 0 END) AS TotalCompletions,
        SUM(CASE WHEN CompletionStatus = 'In Progress' THEN 1 ELSE 0 END) AS TotalInProgress,
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
            // No-op: Down would have to reintroduce the buggy scoping condition, which is never
            // desirable - if this migration needs rolling back, roll back to before it entirely
            // rather than restoring the bug.
        }
    }
}
