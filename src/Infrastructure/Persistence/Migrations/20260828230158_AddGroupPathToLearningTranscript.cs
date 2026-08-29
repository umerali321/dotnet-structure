using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupPathToLearningTranscript : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GroupPath",
                table: "LearningTranscriptIdentities",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // SQL Server table types can't be ALTERed - the only way to add a column is drop and
            // recreate, which means dropping (then recreating) sp_ImportLearningTranscriptBatch
            // too, since it takes this type as a parameter and SQL Server won't drop a type a live
            // procedure still references.
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_ImportLearningTranscriptBatch;");
            migrationBuilder.Sql("DROP TYPE dbo.LearningTranscriptRowType;");

            migrationBuilder.Sql(@"
CREATE TYPE dbo.LearningTranscriptRowType AS TABLE
(
    SkillportUsername             NVARCHAR(100)  NOT NULL,
    SkillportUserIdText           NVARCHAR(50)   NULL,
    FirstName                     NVARCHAR(100)  NULL,
    LastName                      NVARCHAR(100)  NULL,
    DisplayFirstName              NVARCHAR(100)  NULL,
    DisplayLastName               NVARCHAR(100)  NULL,
    Location                      NVARCHAR(200)  NULL,
    UserStatus                    NVARCHAR(50)   NULL,
    GroupName                     NVARCHAR(200)  NULL,
    GroupOrgCode                  NVARCHAR(100)  NULL,
    GroupPath                     NVARCHAR(500)  NULL,
    AssetId                       NVARCHAR(200)  NOT NULL,
    AssetTitle                    NVARCHAR(500)  NOT NULL,
    AssetType                     NVARCHAR(100)  NULL,
    AssetSubType                  NVARCHAR(100)  NULL,
    TimesRestarted                INT            NULL,
    AbsoluteFirstAccessDate       DATE           NULL,
    AbsoluteLastAccessDate        DATE           NULL,
    AbsoluteTimesAccessed         INT            NULL,
    AbsoluteHighScore             DECIMAL(5,2)   NULL,
    AbsoluteLastScore             DECIMAL(5,2)   NULL,
    AbsoluteActualDurationMinutes INT            NULL,
    FirstAccessDate               DATE           NULL,
    LastAccessDate                DATE           NULL,
    TimesAccessed                 INT            NULL,
    TimesDownloaded               INT            NULL,
    DownloadDate                  DATE           NULL,
    HtmlPageReads                 INT            NULL,
    EnrollmentDate                DATE           NULL,
    CompletionDate                DATE           NULL,
    CompletionStatus              NVARCHAR(50)   NULL,
    PreTestScore                  DECIMAL(5,2)   NULL,
    MaxTestAttempts               INT            NULL,
    ActualTestAttempts            INT            NULL,
    HighScore                     DECIMAL(5,2)   NULL,
    CurrentScore                  DECIMAL(5,2)   NULL,
    ExpectedDurationMinutes       INT            NULL,
    ActualDurationMinutes         INT            NULL,
    LastSkillportLoginDate        DATE           NULL,
    SkillportRegistrationDate     DATE           NULL,
    ApprovalManagerId             NVARCHAR(100)  NULL,
    ApprovalManagerFirstName      NVARCHAR(100)  NULL,
    ApprovalManagerLastName       NVARCHAR(100)  NULL,
    EmailAddress                  NVARCHAR(320)  NULL
);
");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GroupPath",
                table: "LearningTranscriptIdentities");
        }
    }
}
