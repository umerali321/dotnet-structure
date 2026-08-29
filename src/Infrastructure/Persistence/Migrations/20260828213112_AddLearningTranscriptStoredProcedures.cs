using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningTranscriptStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.sp_ImportLearningTranscriptBatch
    @Rows dbo.LearningTranscriptRowType READONLY,
    @SourceFileName NVARCHAR(260),
    @ImportedBy NVARCHAR(320)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @Now DATETIMEOFFSET = SYSDATETIMEOFFSET();
        DECLARE @TotalRows INT = (SELECT COUNT(*) FROM @Rows);

        -- 1) Asset dimension: insert new assets, refresh descriptive text + LastSeenAt on ones we
        -- already know about (title/type can legitimately change in the source catalog over time).
        MERGE dbo.LearningTranscriptAssets AS target
        USING (
            SELECT AssetId, MAX(AssetTitle) AS AssetTitle, MAX(AssetType) AS AssetType, MAX(AssetSubType) AS AssetSubType
            FROM @Rows
            GROUP BY AssetId
        ) AS src
        ON target.AssetId = src.AssetId
        WHEN MATCHED THEN
            UPDATE SET AssetTitle = src.AssetTitle, AssetType = src.AssetType, AssetSubType = src.AssetSubType, LastSeenAt = @Now
        WHEN NOT MATCHED THEN
            INSERT (AssetId, AssetTitle, AssetType, AssetSubType, InternalCourseId, FirstSeenAt, LastSeenAt)
            VALUES (
                src.AssetId, src.AssetTitle, src.AssetType, src.AssetSubType,
                (SELECT TOP 1 c.CourseID FROM dbo.Courses c WHERE c.SkillsoftCourseCode = src.AssetId),
                @Now, @Now
            );

        -- 2) Identity dimension: one row per distinct Skillport username in this batch.
        DECLARE @BatchIdentities TABLE (
            SkillportUsername NVARCHAR(100) PRIMARY KEY,
            FirstName NVARCHAR(100), LastName NVARCHAR(100),
            DisplayFirstName NVARCHAR(100), DisplayLastName NVARCHAR(100),
            Location NVARCHAR(200), UserStatus NVARCHAR(50),
            GroupName NVARCHAR(200), GroupOrgCode NVARCHAR(100),
            ApprovalManagerId NVARCHAR(100), ApprovalManagerFirstName NVARCHAR(100), ApprovalManagerLastName NVARCHAR(100)
        );

        INSERT INTO @BatchIdentities
        SELECT SkillportUsername,
               MAX(FirstName), MAX(LastName), MAX(DisplayFirstName), MAX(DisplayLastName),
               MAX(Location), MAX(UserStatus), MAX(GroupName), MAX(GroupOrgCode),
               MAX(ApprovalManagerId), MAX(ApprovalManagerFirstName), MAX(ApprovalManagerLastName)
        FROM @Rows
        GROUP BY SkillportUsername;

        -- Match order: our own normalized SkillportSessions first (covers both the new
        -- 10LC###### accounts and older accounts once adopted), then the legacy ActiveLibraryCards
        -- table via its Email -> Users.Email. Anything neither resolves stays NULL (unmatched,
        -- reviewed manually) rather than guessing by name.
        DECLARE @Resolutions TABLE (
            SkillportUsername NVARCHAR(100) PRIMARY KEY,
            UserId INT NULL,
            ResolutionMethod NVARCHAR(50) NULL
        );

        INSERT INTO @Resolutions (SkillportUsername, UserId, ResolutionMethod)
        SELECT b.SkillportUsername, COALESCE(bySession.UserId, byCard.UserId), COALESCE(bySession.ResolutionMethod, byCard.ResolutionMethod)
        FROM @BatchIdentities b
        OUTER APPLY (
            SELECT TOP 1 ss.UserId, 'SkillportSession' AS ResolutionMethod
            FROM dbo.SkillportSessions ss
            WHERE ss.SkillportUsername = b.SkillportUsername
            ORDER BY ss.CreatedAt DESC
        ) bySession
        OUTER APPLY (
            SELECT TOP 1 u.UserId, 'ActiveLibraryCard' AS ResolutionMethod
            FROM dbo.ActiveLibraryCards alc
            JOIN dbo.Users u ON u.Email = alc.Email
            -- ActiveLibraryCards' underlying column is literally named User_ID (see
            -- ActiveLibraryCardConfiguration.cs's HasColumnName mapping) despite the C# property
            -- being UserId - this raw SQL has to use the real column name.
            WHERE alc.User_ID = b.SkillportUsername AND alc.Email IS NOT NULL
        ) byCard;

        -- Refresh descriptive fields on identities we already know about.
        UPDATE i
        SET i.FirstName = b.FirstName, i.LastName = b.LastName,
            i.DisplayFirstName = b.DisplayFirstName, i.DisplayLastName = b.DisplayLastName,
            i.Location = b.Location, i.UserStatus = b.UserStatus,
            i.GroupName = b.GroupName, i.GroupOrgCode = b.GroupOrgCode,
            i.ApprovalManagerId = b.ApprovalManagerId,
            i.ApprovalManagerFirstName = b.ApprovalManagerFirstName,
            i.ApprovalManagerLastName = b.ApprovalManagerLastName,
            i.UpdatedAt = @Now
        FROM dbo.LearningTranscriptIdentities i
        JOIN @BatchIdentities b ON b.SkillportUsername = i.SkillportUsername;

        -- Re-attempt resolution for existing-but-still-unmatched identities - a matching session
        -- or card may have appeared since the last import.
        UPDATE i
        SET i.UserId = r.UserId, i.ResolutionMethod = r.ResolutionMethod, i.ResolvedAt = @Now
        FROM dbo.LearningTranscriptIdentities i
        JOIN @Resolutions r ON r.SkillportUsername = i.SkillportUsername
        WHERE i.UserId IS NULL AND r.UserId IS NOT NULL;

        -- Insert brand-new identities, resolved at insert time.
        INSERT INTO dbo.LearningTranscriptIdentities (
            SkillportUsername, UserId, FirstName, LastName, DisplayFirstName, DisplayLastName,
            Location, UserStatus, GroupName, GroupOrgCode,
            ApprovalManagerId, ApprovalManagerFirstName, ApprovalManagerLastName,
            ResolutionMethod, ResolvedAt, CreatedAt
        )
        SELECT
            b.SkillportUsername, r.UserId, b.FirstName, b.LastName, b.DisplayFirstName, b.DisplayLastName,
            b.Location, b.UserStatus, b.GroupName, b.GroupOrgCode,
            b.ApprovalManagerId, b.ApprovalManagerFirstName, b.ApprovalManagerLastName,
            r.ResolutionMethod, CASE WHEN r.UserId IS NOT NULL THEN @Now ELSE NULL END, @Now
        FROM @BatchIdentities b
        JOIN @Resolutions r ON r.SkillportUsername = b.SkillportUsername
        WHERE NOT EXISTS (SELECT 1 FROM dbo.LearningTranscriptIdentities i WHERE i.SkillportUsername = b.SkillportUsername);

        DECLARE @MatchedCount INT, @UnmatchedCount INT;
        SELECT
            @MatchedCount = ISNULL(SUM(CASE WHEN i.UserId IS NOT NULL THEN 1 ELSE 0 END), 0),
            @UnmatchedCount = ISNULL(SUM(CASE WHEN i.UserId IS NULL THEN 1 ELSE 0 END), 0)
        FROM @BatchIdentities b
        JOIN dbo.LearningTranscriptIdentities i ON i.SkillportUsername = b.SkillportUsername;

        DECLARE @NewBatchId INT;
        INSERT INTO dbo.LearningTranscriptImportBatches (SourceFileName, ImportedAt, ImportedBy, TotalRows, MatchedCount, UnmatchedCount)
        VALUES (@SourceFileName, @Now, @ImportedBy, @TotalRows, @MatchedCount, @UnmatchedCount);
        SET @NewBatchId = SCOPE_IDENTITY();

        -- De-duplicate defensively in case the source file repeats a (person, course) pair - keeps
        -- the unique-filtered IsLatest index from ever being violated by this batch itself.
        ;WITH DedupedRows AS (
            SELECT r.*, ROW_NUMBER() OVER (PARTITION BY r.SkillportUsername, r.AssetId ORDER BY (SELECT NULL)) AS RowRank
            FROM @Rows r
        )
        -- 3) Supersede whatever was previously 'current' for every (person, course) pair this
        -- batch touches, before inserting the new current rows.
        UPDATE act
        SET act.IsLatest = 0
        FROM dbo.LearningTranscriptActivities act
        JOIN dbo.LearningTranscriptIdentities i ON i.LearningTranscriptIdentityId = act.LearningTranscriptIdentityId
        JOIN (SELECT DISTINCT SkillportUsername, AssetId FROM DedupedRows) d
            ON d.SkillportUsername = i.SkillportUsername AND d.AssetId = act.AssetId
        WHERE act.IsLatest = 1;

        ;WITH DedupedRows AS (
            SELECT r.*, ROW_NUMBER() OVER (PARTITION BY r.SkillportUsername, r.AssetId ORDER BY (SELECT NULL)) AS RowRank
            FROM @Rows r
        )
        INSERT INTO dbo.LearningTranscriptActivities (
            ImportBatchId, LearningTranscriptIdentityId, AssetId, IsLatest,
            TimesRestarted, AbsoluteFirstAccessDate, AbsoluteLastAccessDate, AbsoluteTimesAccessed,
            AbsoluteHighScore, AbsoluteLastScore, AbsoluteActualDurationMinutes,
            FirstAccessDate, LastAccessDate, TimesAccessed, TimesDownloaded, DownloadDate, HtmlPageReads,
            EnrollmentDate, CompletionDate, CompletionStatus, PreTestScore, MaxTestAttempts, ActualTestAttempts,
            HighScore, CurrentScore, ExpectedDurationMinutes, ActualDurationMinutes,
            LastSkillportLoginDate, SkillportRegistrationDate, CreatedAt
        )
        SELECT
            @NewBatchId, i.LearningTranscriptIdentityId, r.AssetId, 1,
            r.TimesRestarted, r.AbsoluteFirstAccessDate, r.AbsoluteLastAccessDate, r.AbsoluteTimesAccessed,
            r.AbsoluteHighScore, r.AbsoluteLastScore, r.AbsoluteActualDurationMinutes,
            r.FirstAccessDate, r.LastAccessDate, r.TimesAccessed, r.TimesDownloaded, r.DownloadDate, r.HtmlPageReads,
            r.EnrollmentDate, r.CompletionDate, r.CompletionStatus, r.PreTestScore, r.MaxTestAttempts, r.ActualTestAttempts,
            r.HighScore, r.CurrentScore, r.ExpectedDurationMinutes, r.ActualDurationMinutes,
            r.LastSkillportLoginDate, r.SkillportRegistrationDate, @Now
        FROM DedupedRows r
        JOIN dbo.LearningTranscriptIdentities i ON i.SkillportUsername = r.SkillportUsername
        WHERE r.RowRank = 1;

        SELECT @NewBatchId AS ImportBatchId, @TotalRows AS TotalRows, @MatchedCount AS MatchedCount, @UnmatchedCount AS UnmatchedCount;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.sp_ListLearningTranscript
    @RestrictToCompanyIds NVARCHAR(MAX) = NULL, -- comma-separated CompanyIds, NULL = no restriction
    @RestrictToUserId INT = NULL,               -- Employee self-view: forces exactly this user
    @RestrictToManagerId INT = NULL,            -- Manager view: narrows to their assigned employees
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
      AND (@RestrictToManagerId IS NULL OR sp.ManagerId = @RestrictToManagerId)
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

    -- Same scoping rules as sp_ListLearningTranscript, aggregated instead of paginated - powers
    -- the report screen's KPI cards over the exact same visible set.
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
          AND (@RestrictToManagerId IS NULL OR sp.ManagerId = @RestrictToManagerId)
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
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_LearningTranscriptStats;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_ListLearningTranscript;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_ImportLearningTranscriptBatch;");
        }
    }
}
