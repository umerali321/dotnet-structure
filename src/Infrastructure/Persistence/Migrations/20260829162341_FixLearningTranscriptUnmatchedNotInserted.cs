using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixLearningTranscriptUnmatchedNotInserted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Two changes to sp_ImportLearningTranscriptBatch:
            // 1. Only identities that actually resolved to a real Users.UserId get their course
            //    activity rows inserted - an unmatched identity (no counterpart in SkillportSessions
            //    or ActiveLibraryCards, e.g. a Skillport-side test/demo group with no corresponding
            //    Company in this system at all) is still recorded in LearningTranscriptIdentities for
            //    visibility, but its activity data is no longer persisted into the report.
            // 2. The summary row now also returns the distinct Group Org Codes among unmatched
            //    identities in this batch - the actual, concrete reason rows didn't import (an entire
            //    unrecognized company/group), rather than a bare count with no explanation.
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

        DECLARE @BatchIdentities TABLE (
            SkillportUsername NVARCHAR(100) PRIMARY KEY,
            FirstName NVARCHAR(100), LastName NVARCHAR(100),
            DisplayFirstName NVARCHAR(100), DisplayLastName NVARCHAR(100),
            Location NVARCHAR(200), UserStatus NVARCHAR(50),
            GroupName NVARCHAR(200), GroupOrgCode NVARCHAR(100), GroupPath NVARCHAR(500),
            ApprovalManagerId NVARCHAR(100), ApprovalManagerFirstName NVARCHAR(100), ApprovalManagerLastName NVARCHAR(100)
        );

        INSERT INTO @BatchIdentities
        SELECT SkillportUsername,
               MAX(FirstName), MAX(LastName), MAX(DisplayFirstName), MAX(DisplayLastName),
               MAX(Location), MAX(UserStatus), MAX(GroupName), MAX(GroupOrgCode), MAX(GroupPath),
               MAX(ApprovalManagerId), MAX(ApprovalManagerFirstName), MAX(ApprovalManagerLastName)
        FROM @Rows
        GROUP BY SkillportUsername;

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
            WHERE alc.User_ID = b.SkillportUsername AND alc.Email IS NOT NULL
        ) byCard;

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
        JOIN @BatchIdentities b ON b.SkillportUsername = i.SkillportUsername;

        UPDATE i
        SET i.UserId = r.UserId, i.ResolutionMethod = r.ResolutionMethod, i.ResolvedAt = @Now
        FROM dbo.LearningTranscriptIdentities i
        JOIN @Resolutions r ON r.SkillportUsername = i.SkillportUsername
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
        FROM @BatchIdentities b
        JOIN @Resolutions r ON r.SkillportUsername = b.SkillportUsername
        WHERE NOT EXISTS (SELECT 1 FROM dbo.LearningTranscriptIdentities i WHERE i.SkillportUsername = b.SkillportUsername);

        DECLARE @MatchedCount INT, @UnmatchedCount INT, @UnmatchedGroupOrgCodes NVARCHAR(MAX);
        SELECT
            @MatchedCount = ISNULL(SUM(CASE WHEN i.UserId IS NOT NULL THEN 1 ELSE 0 END), 0),
            @UnmatchedCount = ISNULL(SUM(CASE WHEN i.UserId IS NULL THEN 1 ELSE 0 END), 0)
        FROM @BatchIdentities b
        JOIN dbo.LearningTranscriptIdentities i ON i.SkillportUsername = b.SkillportUsername;

        SELECT @UnmatchedGroupOrgCodes = STRING_AGG(GroupOrgCode, ', ')
        FROM (
            SELECT DISTINCT i.GroupOrgCode
            FROM @BatchIdentities b
            JOIN dbo.LearningTranscriptIdentities i ON i.SkillportUsername = b.SkillportUsername
            WHERE i.UserId IS NULL AND i.GroupOrgCode IS NOT NULL
        ) codes;

        DECLARE @NewBatchId INT;
        INSERT INTO dbo.LearningTranscriptImportBatches (SourceFileName, ImportedAt, ImportedBy, TotalRows, MatchedCount, UnmatchedCount)
        VALUES (@SourceFileName, @Now, @ImportedBy, @TotalRows, @MatchedCount, @UnmatchedCount);
        SET @NewBatchId = SCOPE_IDENTITY();

        ;WITH DedupedRows AS (
            SELECT r.*, ROW_NUMBER() OVER (PARTITION BY r.SkillportUsername, r.AssetId ORDER BY (SELECT NULL)) AS RowRank
            FROM @Rows r
        )
        UPDATE act
        SET act.IsLatest = 0
        FROM dbo.LearningTranscriptActivities act
        JOIN dbo.LearningTranscriptIdentities i ON i.LearningTranscriptIdentityId = act.LearningTranscriptIdentityId
        JOIN (SELECT DISTINCT SkillportUsername, AssetId FROM DedupedRows) d
            ON d.SkillportUsername = i.SkillportUsername AND d.AssetId = act.AssetId
        WHERE act.IsLatest = 1;

        -- Only identities that resolved to a real internal Users.UserId get their activity rows
        -- persisted (i.UserId IS NOT NULL) - an identity with no counterpart in this system (no
        -- SkillportSessions/ActiveLibraryCards match) is recorded above for visibility, but its
        -- course activity is not inserted into the report data itself.
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
        WHERE r.RowRank = 1 AND i.UserId IS NOT NULL;

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

        DECLARE @BatchIdentities TABLE (
            SkillportUsername NVARCHAR(100) PRIMARY KEY,
            FirstName NVARCHAR(100), LastName NVARCHAR(100),
            DisplayFirstName NVARCHAR(100), DisplayLastName NVARCHAR(100),
            Location NVARCHAR(200), UserStatus NVARCHAR(50),
            GroupName NVARCHAR(200), GroupOrgCode NVARCHAR(100), GroupPath NVARCHAR(500),
            ApprovalManagerId NVARCHAR(100), ApprovalManagerFirstName NVARCHAR(100), ApprovalManagerLastName NVARCHAR(100)
        );

        INSERT INTO @BatchIdentities
        SELECT SkillportUsername,
               MAX(FirstName), MAX(LastName), MAX(DisplayFirstName), MAX(DisplayLastName),
               MAX(Location), MAX(UserStatus), MAX(GroupName), MAX(GroupOrgCode), MAX(GroupPath),
               MAX(ApprovalManagerId), MAX(ApprovalManagerFirstName), MAX(ApprovalManagerLastName)
        FROM @Rows
        GROUP BY SkillportUsername;

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
            WHERE alc.User_ID = b.SkillportUsername AND alc.Email IS NOT NULL
        ) byCard;

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
        JOIN @BatchIdentities b ON b.SkillportUsername = i.SkillportUsername;

        UPDATE i
        SET i.UserId = r.UserId, i.ResolutionMethod = r.ResolutionMethod, i.ResolvedAt = @Now
        FROM dbo.LearningTranscriptIdentities i
        JOIN @Resolutions r ON r.SkillportUsername = i.SkillportUsername
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

        ;WITH DedupedRows AS (
            SELECT r.*, ROW_NUMBER() OVER (PARTITION BY r.SkillportUsername, r.AssetId ORDER BY (SELECT NULL)) AS RowRank
            FROM @Rows r
        )
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
        }
    }
}
