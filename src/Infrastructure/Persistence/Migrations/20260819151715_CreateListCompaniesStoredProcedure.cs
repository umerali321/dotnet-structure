using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateListCompaniesStoredProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.sp_ListCompanies
    @Search NVARCHAR(200) = NULL,
    @IncludeInactive BIT = 0,
    @RestrictToCompanyIds NVARCHAR(MAX) = NULL, -- comma-separated CompanyIds, NULL = no restriction
    @Page INT = 1,
    @PageSize INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATE = CAST(GETUTCDATE() AS DATE);
    DECLARE @Term NVARCHAR(204) = NULL;
    IF @Search IS NOT NULL AND LTRIM(RTRIM(@Search)) <> ''
        SET @Term = '%' + REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(@Search)), '\', '\\'), '%', '\%'), '_', '\_') + '%';

    SELECT
        c.CompanyId, c.CompanyCode, c.CompanyName, c.IsActive, c.LogoUrl,
        c.PlanType, c.PlanStartDate, c.PlanEndDate,
        CAST(CASE WHEN c.PlanEndDate < @Today THEN 1 ELSE 0 END AS BIT) AS IsExpired,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.Companies c
    WHERE (@IncludeInactive = 1 OR (c.IsActive = 1 AND c.PlanEndDate >= @Today))
      AND (@RestrictToCompanyIds IS NULL OR c.CompanyId IN (SELECT CAST(value AS INT) FROM STRING_SPLIT(@RestrictToCompanyIds, ',')))
      AND (@Term IS NULL OR c.CompanyName LIKE @Term ESCAPE '\' OR c.CompanyCode LIKE @Term ESCAPE '\')
    ORDER BY c.CompanyName
    OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_ListCompanies");
        }
    }
}
