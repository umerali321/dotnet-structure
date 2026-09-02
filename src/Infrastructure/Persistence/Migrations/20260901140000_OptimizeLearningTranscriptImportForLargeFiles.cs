using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Fixes a hard failure and rebuilds the import for real file sizes.
    ///
    /// THE BUG: STRING_AGG returns NVARCHAR(4000) unless its input is a LOB, and throws
    /// "aggregation result exceeded the limit of 8000 bytes" the moment the concatenated list is
    /// longer. With a 50,000-row file the unmatched group-code list blew past that and the whole
    /// import 500'd - so nothing imported at all, for a purely cosmetic summary field.
    ///
    /// THE SLOWNESS: the procedure was written against table variables (@BatchIdentities,
    /// @Resolutions) and a per-identity OUTER APPLY over dbo.Users. Table variables carry no
    /// statistics, so SQL Server estimates ONE row and picks nested loops everywhere; and the name
    /// match re-scanned all ~162,000 Users rows once per identity. At a few hundred rows nobody
    /// notices. At 50,000 it is hours of work the optimizer never had a chance to plan properly.
    ///
    /// The rewrite keeps the behaviour identical and changes only how the work is expressed:
    /// temp tables (which DO have statistics) instead of table variables, the three identity
    /// lookups pre-computed set-based instead of row-by-row, the dedupe window function
    /// materialised once instead of twice, and all of that prepared BEFORE the transaction opens so
    /// locks are held for the writes alone.
    /// </summary>
    public partial class OptimizeLearningTranscriptImportForLargeFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Supporting indexes. Without the Users(FirstName, LastName) one the name-match step has
            // no choice but to scan the whole table; the other two turn the session/card lookups
            // into seeks. Guarded so re-running is harmless.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_FirstName_LastName' AND object_id = OBJECT_ID('dbo.Users'))
    CREATE NONCLUSTERED INDEX IX_Users_FirstName_LastName
        ON dbo.Users (FirstName, LastName) INCLUDE (UserId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SkillportSessions_Username_Created' AND object_id = OBJECT_ID('dbo.SkillportSessions'))
    CREATE NONCLUSTERED INDEX IX_SkillportSessions_Username_Created
        ON dbo.SkillportSessions (SkillportUsername, CreatedAt DESC) INCLUDE (UserId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ActiveLibraryCards_UserId' AND object_id = OBJECT_ID('dbo.ActiveLibraryCards'))
    CREATE NONCLUSTERED INDEX IX_ActiveLibraryCards_UserId
        ON dbo.ActiveLibraryCards (User_ID) INCLUDE (Email);
");

            migrationBuilder.Sql(@"CREATE OR ALTER PROCEDURE dbo.sp_ImportLearningTranscriptBatch
    @Rows dbo.LearningTranscriptRowType READONLY,
    @SourceFileName NVARCHAR(260),
    @ImportedBy NVARCHAR(320)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        DECLARE @Now DATETIMEOFFSET = SYSDATETIMEOFFSET();
        DECLARE @TotalRows INT = (SELECT COUNT(*) FROM @Rows);

        -- ================= PREPARATION (deliberately OUTSIDE the transaction) =================
        -- None of this writes to a real table, so holding locks through it would block other
        -- readers for the entire duration of a large import for no benefit.

        -- The TVP is a table variable: no statistics, estimated at one row. Everything downstream
        -- joins against it, so it is copied once into an indexed temp table and the per-(user,asset)
        -- dedupe rank is computed here ONCE rather than in two separate CTEs later.
        SELECT
            r.*,
            ROW_NUMBER() OVER (PARTITION BY r.SkillportUsername, r.AssetId ORDER BY (SELECT NULL)) AS RowRank
        INTO #Rows
        FROM @Rows r;

        CREATE CLUSTERED INDEX IX_Rows_User_Asset ON #Rows (SkillportUsername, AssetId);

        SELECT SkillportUsername,
               MAX(FirstName) AS FirstName, MAX(LastName) AS LastName,
               MAX(DisplayFirstName) AS DisplayFirstName, MAX(DisplayLastName) AS DisplayLastName,
               MAX(Location) AS Location, MAX(UserStatus) AS UserStatus,
               MAX(GroupName) AS GroupName, MAX(GroupOrgCode) AS GroupOrgCode, MAX(GroupPath) AS GroupPath,
               MAX(ApprovalManagerId) AS ApprovalManagerId,
               MAX(ApprovalManagerFirstName) AS ApprovalManagerFirstName,
               MAX(ApprovalManagerLastName) AS ApprovalManagerLastName
        INTO #BatchIdentities
        FROM #Rows
        GROUP BY SkillportUsername;

        CREATE UNIQUE CLUSTERED INDEX IX_BatchIdentities ON #BatchIdentities (SkillportUsername);

        -- ---- the three identity lookups, each computed set-based over the whole batch ----
        -- These were OUTER APPLYs evaluated once per identity. The card and session lookups were
        -- merely wasteful; the name match was quadratic - COUNT(*) OVER () across dbo.Users for
        -- every single identity in the file.

        SELECT SkillportUsername, UserId
        INTO #BySession
        FROM (
            SELECT ss.SkillportUsername, ss.UserId,
                   ROW_NUMBER() OVER (PARTITION BY ss.SkillportUsername ORDER BY ss.CreatedAt DESC) AS rn
            FROM dbo.SkillportSessions ss
            JOIN #BatchIdentities b ON b.SkillportUsername = ss.SkillportUsername
        ) x
        WHERE rn = 1;

        CREATE UNIQUE CLUSTERED INDEX IX_BySession ON #BySession (SkillportUsername);

        SELECT b.SkillportUsername, MIN(u.UserId) AS UserId
        INTO #ByCard
        FROM #BatchIdentities b
        JOIN dbo.ActiveLibraryCards alc ON alc.User_ID = b.SkillportUsername AND alc.Email IS NOT NULL
        JOIN dbo.Users u ON u.Email = alc.Email
        GROUP BY b.SkillportUsername;

        CREATE UNIQUE CLUSTERED INDEX IX_ByCard ON #ByCard (SkillportUsername);

        -- Only names that identify EXACTLY ONE user are usable - the same rule the old
        -- MatchCount = 1 filter enforced, now applied once for the whole batch instead of per row.
        SELECT u.FirstName, u.LastName, MIN(u.UserId) AS UserId
        INTO #ByName
        FROM dbo.Users u
        JOIN (
            SELECT DISTINCT FirstName, LastName
            FROM #BatchIdentities
            WHERE FirstName IS NOT NULL AND LastName IS NOT NULL
        ) n ON n.FirstName = u.FirstName AND n.LastName = u.LastName
        GROUP BY u.FirstName, u.LastName
        HAVING COUNT(*) = 1;

        CREATE UNIQUE CLUSTERED INDEX IX_ByName ON #ByName (FirstName, LastName);

        -- Precedence is unchanged: SkillportSession, then ActiveLibraryCard, then a unique name.
        SELECT
            b.SkillportUsername,
            COALESCE(s.UserId, c.UserId, n.UserId) AS UserId,
            CASE
                WHEN s.UserId IS NOT NULL THEN 'SkillportSession'
                WHEN c.UserId IS NOT NULL THEN 'ActiveLibraryCard'
                WHEN n.UserId IS NOT NULL THEN 'NameMatch'
            END AS ResolutionMethod
        INTO #Resolutions
        FROM #BatchIdentities b
        LEFT JOIN #BySession s ON s.SkillportUsername = b.SkillportUsername
        LEFT JOIN #ByCard    c ON c.SkillportUsername = b.SkillportUsername
        LEFT JOIN #ByName    n ON n.FirstName = b.FirstName AND n.LastName = b.LastName;

        CREATE UNIQUE CLUSTERED INDEX IX_Resolutions ON #Resolutions (SkillportUsername);

        -- ================= WRITES =================
        BEGIN TRANSACTION;

        MERGE dbo.LearningTranscriptAssets AS target
        USING (
            SELECT AssetId, MAX(AssetTitle) AS AssetTitle, MAX(AssetType) AS AssetType, MAX(AssetSubType) AS AssetSubType
            FROM #Rows
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

        UPDATE i
        SET i.FirstName = b.FirstName, i.LastName = b.LastName,
            i.DisplayFirstName = b.DisplayFirstName, i.DisplayLastName = b.DisplayLastName,
            i.Location = b.Location, i.UserStatus = b.UserStatus,
            i.GroupName = b.GroupName, i.GroupOrgCode = b.GroupOrgCode, i.GroupPath = b.GroupPath,
            i.ApprovalManagerId = b.ApprovalManagerId,
            i.ApprovalManagerFirstName = b.ApprovalManagerFirstName,
            i.ApprovalManagerLastName = b.ApprovalManagerLastName,
            i.UpdatedAt = @Now
        FROM dbo.LearningTranscriptIdentities i
        JOIN #BatchIdentities b ON b.SkillportUsername = i.SkillportUsername;

        UPDATE i
        SET i.UserId = r.UserId, i.ResolutionMethod = r.ResolutionMethod, i.ResolvedAt = @Now
        FROM dbo.LearningTranscriptIdentities i
        JOIN #Resolutions r ON r.SkillportUsername = i.SkillportUsername
        WHERE i.UserId IS NULL AND r.UserId IS NOT NULL;

        INSERT INTO dbo.LearningTranscriptIdentities (
            SkillportUsername, UserId, FirstName, LastName, DisplayFirstName, DisplayLastName,
            Location, UserStatus, GroupName, GroupOrgCode, GroupPath,
            ApprovalManagerId, ApprovalManagerFirstName, ApprovalManagerLastName,
            ResolutionMethod, ResolvedAt, CreatedAt
        )
        SELECT
            b.SkillportUsername, r.UserId, b.FirstName, b.LastName, b.DisplayFirstName, b.DisplayLastName,
            b.Location, b.UserStatus, b.GroupName, b.GroupOrgCode, b.GroupPath,
            b.ApprovalManagerId, b.ApprovalManagerFirstName, b.ApprovalManagerLastName,
            r.ResolutionMethod, CASE WHEN r.UserId IS NOT NULL THEN @Now ELSE NULL END, @Now
        FROM #BatchIdentities b
        JOIN #Resolutions r ON r.SkillportUsername = b.SkillportUsername
        WHERE NOT EXISTS (SELECT 1 FROM dbo.LearningTranscriptIdentities i WHERE i.SkillportUsername = b.SkillportUsername);

        DECLARE @MatchedCount INT, @UnmatchedCount INT, @UnmatchedGroupOrgCodes NVARCHAR(MAX);
        SELECT
            @MatchedCount = ISNULL(SUM(CASE WHEN i.UserId IS NOT NULL THEN 1 ELSE 0 END), 0),
            @UnmatchedCount = ISNULL(SUM(CASE WHEN i.UserId IS NULL THEN 1 ELSE 0 END), 0)
        FROM #BatchIdentities b
        JOIN dbo.LearningTranscriptIdentities i ON i.SkillportUsername = b.SkillportUsername;

        -- THE FIX. Two things were wrong:
        --   1. CAST to NVARCHAR(MAX) - STRING_AGG caps at 8000 bytes unless its input is a LOB, and
        --      threw outright (not truncated - THREW) past that, failing the whole import.
        --   2. Capped at 50 codes. This is a hint shown in a toast; a caller does not want, and the
        --      UI cannot display, a list of every one of thousands of unmatched group codes.
        DECLARE @CodeLimit INT = 50;
        DECLARE @DistinctCodes INT;

        SELECT DISTINCT i.GroupOrgCode
        INTO #UnmatchedCodes
        FROM #BatchIdentities b
        JOIN dbo.LearningTranscriptIdentities i ON i.SkillportUsername = b.SkillportUsername
        WHERE i.UserId IS NULL AND i.GroupOrgCode IS NOT NULL;

        SELECT @DistinctCodes = COUNT(*) FROM #UnmatchedCodes;

        SELECT @UnmatchedGroupOrgCodes = STRING_AGG(CAST(GroupOrgCode AS NVARCHAR(MAX)), ', ')
                                             WITHIN GROUP (ORDER BY GroupOrgCode)
        FROM (SELECT TOP (@CodeLimit) GroupOrgCode FROM #UnmatchedCodes ORDER BY GroupOrgCode) capped;

        IF @DistinctCodes > @CodeLimit
            SET @UnmatchedGroupOrgCodes = @UnmatchedGroupOrgCodes
                + N' ... (+' + CAST(@DistinctCodes - @CodeLimit AS NVARCHAR(20)) + N' more)';

        DECLARE @NewBatchId INT;
        INSERT INTO dbo.LearningTranscriptImportBatches (SourceFileName, ImportedAt, ImportedBy, TotalRows, MatchedCount, UnmatchedCount)
        VALUES (@SourceFileName, @Now, @ImportedBy, @TotalRows, @MatchedCount, @UnmatchedCount);
        SET @NewBatchId = SCOPE_IDENTITY();

        -- Retire the rows the INSERT below replaces. Unchanged in meaning - only identities that
        -- resolved are touched, so an unresolved person's existing history is never silently dropped.
        UPDATE act
        SET act.IsLatest = 0
        FROM dbo.LearningTranscriptActivities act
        JOIN dbo.LearningTranscriptIdentities i ON i.LearningTranscriptIdentityId = act.LearningTranscriptIdentityId
        JOIN (SELECT DISTINCT SkillportUsername, AssetId FROM #Rows) d
            ON d.SkillportUsername = i.SkillportUsername AND d.AssetId = act.AssetId
        WHERE act.IsLatest = 1
          AND i.UserId IS NOT NULL;

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
        FROM #Rows r
        JOIN dbo.LearningTranscriptIdentities i ON i.SkillportUsername = r.SkillportUsername
        WHERE r.RowRank = 1 AND i.UserId IS NOT NULL;

        -- Close out Course Library launches that Skillport now reports as finished. Still the only
        -- thing that completes a CourseTaken row, and TakeCourse allows one active course at a time.
        --
        -- Rewritten from a correlated EXISTS over the WHOLE activities table into a join against a
        -- set bounded by the people in THIS file. Same rule (latest activity, Completed), but it no
        -- longer re-examines every activity ever imported on every run.
        UPDATE ct
        SET ct.IsActive = 0,
            ct.CompletedAt = @Now
        FROM dbo.CourseTakens ct
        JOIN (
            SELECT DISTINCT i.UserId, s.InternalCourseId
            FROM dbo.LearningTranscriptActivities a
            JOIN dbo.LearningTranscriptIdentities i ON i.LearningTranscriptIdentityId = a.LearningTranscriptIdentityId
            JOIN dbo.LearningTranscriptAssets s ON s.AssetId = a.AssetId
            JOIN #BatchIdentities b ON b.SkillportUsername = i.SkillportUsername
            WHERE a.IsLatest = 1
              AND a.CompletionStatus = 'Completed'
              AND i.UserId IS NOT NULL
              AND s.InternalCourseId IS NOT NULL
        ) done ON done.UserId = ct.UserId AND done.InternalCourseId = ct.CourseId
        WHERE ct.IsActive = 1;

        SELECT @NewBatchId AS ImportBatchId, @TotalRows AS TotalRows, @MatchedCount AS MatchedCount,
               @UnmatchedCount AS UnmatchedCount, @UnmatchedGroupOrgCodes AS UnmatchedGroupOrgCodes;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately does NOT restore the previous procedure body: it fails outright on any
            // file large enough to produce more than 8000 bytes of unmatched group codes, so going
            // back to it would reintroduce a hard bug. Only the added indexes are reversed.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_FirstName_LastName' AND object_id = OBJECT_ID('dbo.Users'))
    DROP INDEX IX_Users_FirstName_LastName ON dbo.Users;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SkillportSessions_Username_Created' AND object_id = OBJECT_ID('dbo.SkillportSessions'))
    DROP INDEX IX_SkillportSessions_Username_Created ON dbo.SkillportSessions;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ActiveLibraryCards_UserId' AND object_id = OBJECT_ID('dbo.ActiveLibraryCards'))
    DROP INDEX IX_ActiveLibraryCards_UserId ON dbo.ActiveLibraryCards;
");
        }
    }
}
