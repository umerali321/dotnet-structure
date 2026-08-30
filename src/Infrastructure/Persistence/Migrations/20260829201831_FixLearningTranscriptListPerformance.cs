using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixLearningTranscriptListPerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Confirmed live: a single page-1 request against ~1100 real activity rows took 6+
            // seconds - slow enough that clicking "next page" looked broken (the click worked, the
            // response just hadn't come back yet). Root cause: the old version computed
            // TotalSessions (a correlated subquery against SkillportSessions) and the company lookup
            // (an OUTER APPLY into UserCompanyRoles/Companies) in the SAME SELECT that also computes
            // COUNT(*) OVER() for the pagination total - and COUNT(*) OVER() with no PARTITION BY
            // forces SQL Server to materialize the ENTIRE filtered result set (every matching row
            // across every page) before OFFSET/FETCH ever trims it down to the 50 actually being
            // shown. That meant both of those per-row lookups ran once per matching row platform-wide
            // (over a thousand times), not once per row actually displayed.
            //
            // Fix: paginate first, enrich after. An inner CTE does only the cheap filtering/joins
            // needed for COUNT(*) OVER() and applies its own ORDER BY + OFFSET/FETCH to cut down to
            // the 50 target rows; the outer SELECT (which does the SAME OFFSET/FETCH again, now over
            // only those already-narrow ~50 rows) is where TotalSessions and the company OUTER APPLY
            // run - so they now execute at most PageSize times per request instead of once per row
            // in the whole matched set.
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.sp_ListLearningTranscript
    @RestrictToCompanyIds NVARCHAR(MAX) = NULL,
    @RestrictToUserId INT = NULL,
    @RestrictToManagerId INT = NULL,
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

    ;WITH FilteredPage AS (
        SELECT
            act.LearningTranscriptActivityId,
            u.UserId, u.FirstName AS EmployeeFirstName, u.LastName AS EmployeeLastName, u.Email AS EmployeeEmail,
            sp.ManagerId,
            i.UserStatus, i.GroupName, i.GroupOrgCode, i.GroupPath,
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
    )
    SELECT
        fp.LearningTranscriptActivityId,
        fp.UserId, fp.EmployeeFirstName, fp.EmployeeLastName, fp.EmployeeEmail,
        comp.CompanyId, comp.CompanyName,
        fp.ManagerId, mgr.FirstName AS ManagerFirstName, mgr.LastName AS ManagerLastName,
        fp.UserStatus, fp.GroupName, fp.GroupOrgCode, fp.GroupPath,
        sessionCounts.TotalSessions,
        fp.AssetId, fp.AssetTitle, fp.AssetType, fp.AssetSubType,
        fp.EnrollmentDate, fp.FirstAccessDate, fp.LastAccessDate, fp.CompletionDate, fp.CompletionStatus,
        fp.HighScore, fp.CurrentScore, fp.PreTestScore,
        fp.MaxTestAttempts, fp.ActualTestAttempts,
        fp.ExpectedDurationMinutes, fp.ActualDurationMinutes,
        fp.TimesAccessed, fp.TimesDownloaded, fp.TimesRestarted,
        fp.AbsoluteFirstAccessDate, fp.AbsoluteLastAccessDate, fp.AbsoluteTimesAccessed,
        fp.AbsoluteHighScore, fp.AbsoluteLastScore, fp.AbsoluteActualDurationMinutes,
        fp.LastSkillportLoginDate, fp.SkillportRegistrationDate,
        fp.TotalCount
    FROM FilteredPage fp
    LEFT JOIN dbo.Users mgr ON mgr.UserId = fp.ManagerId
    OUTER APPLY (
        SELECT TOP 1 c.CompanyId, c.CompanyName
        FROM dbo.UserCompanyRoles ucr
        JOIN dbo.Companies c ON c.CompanyId = ucr.CompanyId
        WHERE ucr.UserId = fp.UserId AND ucr.IsActive = 1
        ORDER BY ucr.StartDate DESC
    ) comp
    OUTER APPLY (
        SELECT COUNT(*) AS TotalSessions FROM dbo.SkillportSessions ss2 WHERE ss2.UserId = fp.UserId
    ) sessionCounts
    ORDER BY fp.EmployeeLastName, fp.EmployeeFirstName, fp.AssetTitle
    OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.sp_ListLearningTranscript
    @RestrictToCompanyIds NVARCHAR(MAX) = NULL,
    @RestrictToUserId INT = NULL,
    @RestrictToManagerId INT = NULL,
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
        i.UserStatus, i.GroupName, i.GroupOrgCode, i.GroupPath,
        (SELECT COUNT(*) FROM dbo.SkillportSessions ss2 WHERE ss2.UserId = u.UserId) AS TotalSessions,
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
        }
    }
}
