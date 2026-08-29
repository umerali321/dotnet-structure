using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningTranscriptTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LearningTranscriptActivities",
                columns: table => new
                {
                    LearningTranscriptActivityId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportBatchId = table.Column<int>(type: "int", nullable: false),
                    LearningTranscriptIdentityId = table.Column<int>(type: "int", nullable: false),
                    AssetId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsLatest = table.Column<bool>(type: "bit", nullable: false),
                    TimesRestarted = table.Column<int>(type: "int", nullable: true),
                    AbsoluteFirstAccessDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AbsoluteLastAccessDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AbsoluteTimesAccessed = table.Column<int>(type: "int", nullable: true),
                    AbsoluteHighScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    AbsoluteLastScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    AbsoluteActualDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    FirstAccessDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LastAccessDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TimesAccessed = table.Column<int>(type: "int", nullable: true),
                    TimesDownloaded = table.Column<int>(type: "int", nullable: true),
                    DownloadDate = table.Column<DateOnly>(type: "date", nullable: true),
                    HtmlPageReads = table.Column<int>(type: "int", nullable: true),
                    EnrollmentDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CompletionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CompletionStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PreTestScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    MaxTestAttempts = table.Column<int>(type: "int", nullable: true),
                    ActualTestAttempts = table.Column<int>(type: "int", nullable: true),
                    HighScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    CurrentScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    ExpectedDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    ActualDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    LastSkillportLoginDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SkillportRegistrationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningTranscriptActivities", x => x.LearningTranscriptActivityId);
                });

            migrationBuilder.CreateTable(
                name: "LearningTranscriptAssets",
                columns: table => new
                {
                    AssetId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AssetTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AssetType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AssetSubType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InternalCourseId = table.Column<long>(type: "bigint", nullable: true),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningTranscriptAssets", x => x.AssetId);
                });

            migrationBuilder.CreateTable(
                name: "LearningTranscriptIdentities",
                columns: table => new
                {
                    LearningTranscriptIdentityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SkillportUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DisplayFirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DisplayLastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UserStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GroupName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    GroupOrgCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovalManagerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovalManagerFirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovalManagerLastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ResolutionMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningTranscriptIdentities", x => x.LearningTranscriptIdentityId);
                });

            migrationBuilder.CreateTable(
                name: "LearningTranscriptImportBatches",
                columns: table => new
                {
                    ImportBatchId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ImportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ImportedBy = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    MatchedCount = table.Column<int>(type: "int", nullable: false),
                    UnmatchedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningTranscriptImportBatches", x => x.ImportBatchId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LearningTranscriptActivities_Identity_Asset_History",
                table: "LearningTranscriptActivities",
                columns: new[] { "LearningTranscriptIdentityId", "AssetId", "ImportBatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_LearningTranscriptActivities_Identity_Asset_Latest",
                table: "LearningTranscriptActivities",
                columns: new[] { "LearningTranscriptIdentityId", "AssetId" },
                unique: true,
                filter: "[IsLatest] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_LearningTranscriptActivities_ImportBatchId",
                table: "LearningTranscriptActivities",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningTranscriptAssets_InternalCourseId",
                table: "LearningTranscriptAssets",
                column: "InternalCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningTranscriptIdentities_SkillportUsername",
                table: "LearningTranscriptIdentities",
                column: "SkillportUsername",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningTranscriptIdentities_UserId",
                table: "LearningTranscriptIdentities",
                column: "UserId");

            // Table-valued type used to hand a whole import batch's rows to
            // sp_ImportLearningTranscriptBatch in one call - from the API (a DataTable parsed out
            // of the uploaded .xlsx) or from a hand-written SQL script (a @Rows table variable of
            // this same type, exactly like the pattern already used for the Albemarle roster
            // import). Durations arrive pre-converted to whole minutes (not "H:MM" text) so the
            // stored procedure and the fact table stay simply typed.
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TYPE IF EXISTS dbo.LearningTranscriptRowType;");

            migrationBuilder.DropTable(
                name: "LearningTranscriptActivities");

            migrationBuilder.DropTable(
                name: "LearningTranscriptAssets");

            migrationBuilder.DropTable(
                name: "LearningTranscriptIdentities");

            migrationBuilder.DropTable(
                name: "LearningTranscriptImportBatches");
        }
    }
}
