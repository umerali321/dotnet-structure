using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAddressFieldsToListCompaniesSp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // sp_ListCompanies was created (and later updated for @StatusFilter) before the
            // Email/Phone/address/PaymentForm/TotalPayment columns existed on Companies - it never
            // selected them, so the Companies list screen always showed those fields blank even
            // though they're populated (the Edit Company dialog reads the row directly via EF, not
            // through this procedure, which is why it always had the data).
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.sp_ListCompanies
    @Search NVARCHAR(200) = NULL,
    @IncludeInactive BIT = 0,
    @RestrictToCompanyIds NVARCHAR(MAX) = NULL, -- comma-separated CompanyIds, NULL = no restriction
    @StatusFilter NVARCHAR(20) = NULL, -- 'Trial' | 'License' | 'Expired' | 'Deactivated' | NULL (no filter)
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
        c.CompanyEmail, c.CompanyPhone, c.Street1, c.Street2, c.City, c.State, c.Zip,
        c.PaymentForm, c.TotalPayment,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.Companies c
    WHERE (@IncludeInactive = 1 OR (c.IsActive = 1 AND c.PlanEndDate >= @Today))
      AND (@RestrictToCompanyIds IS NULL OR c.CompanyId IN (SELECT CAST(value AS INT) FROM STRING_SPLIT(@RestrictToCompanyIds, ',')))
      AND (@Term IS NULL OR c.CompanyName LIKE @Term ESCAPE '\' OR c.CompanyCode LIKE @Term ESCAPE '\')
      AND (
        @StatusFilter IS NULL
        OR (@StatusFilter = 'Deactivated' AND c.IsActive = 0)
        OR (@StatusFilter = 'Expired' AND c.IsActive = 1 AND c.PlanEndDate < @Today)
        OR (@StatusFilter = 'Trial' AND c.IsActive = 1 AND c.PlanEndDate >= @Today AND c.PlanType = 'Trial')
        OR (@StatusFilter = 'License' AND c.IsActive = 1 AND c.PlanEndDate >= @Today AND c.PlanType = 'License')
      )
    ORDER BY c.CompanyName
    OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.sp_ListCompanies
    @Search NVARCHAR(200) = NULL,
    @IncludeInactive BIT = 0,
    @RestrictToCompanyIds NVARCHAR(MAX) = NULL,
    @StatusFilter NVARCHAR(20) = NULL,
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
      AND (
        @StatusFilter IS NULL
        OR (@StatusFilter = 'Deactivated' AND c.IsActive = 0)
        OR (@StatusFilter = 'Expired' AND c.IsActive = 1 AND c.PlanEndDate < @Today)
        OR (@StatusFilter = 'Trial' AND c.IsActive = 1 AND c.PlanEndDate >= @Today AND c.PlanType = 'Trial')
        OR (@StatusFilter = 'License' AND c.IsActive = 1 AND c.PlanEndDate >= @Today AND c.PlanType = 'License')
      )
    ORDER BY c.CompanyName
    OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END");
        }
    }
}
