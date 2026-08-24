using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateDashboardStatsStoredProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.sp_GetDashboardStats
    @RestrictToCompanyIds NVARCHAR(MAX) = NULL, -- comma-separated CompanyIds, NULL = no restriction (an empty string means ""restricted to zero companies"")
    @StartDate DATE = NULL,
    @EndDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATE = CAST(GETUTCDATE() AS DATE);
    DECLARE @ThirtyDaysOut DATE = DATEADD(DAY, 30, @Today);
    DECLARE @HasRestriction BIT = CASE WHEN @RestrictToCompanyIds IS NULL THEN 0 ELSE 1 END;

    DECLARE @CompanyIds TABLE (CompanyId INT PRIMARY KEY);
    IF @HasRestriction = 1 AND LEN(LTRIM(RTRIM(@RestrictToCompanyIds))) > 0
        INSERT INTO @CompanyIds (CompanyId) SELECT CAST(value AS INT) FROM STRING_SPLIT(@RestrictToCompanyIds, ',');

    ;WITH ScopedCompanies AS (
        SELECT c.CompanyId, c.CompanyCode, c.IsActive, c.PlanType, c.PlanEndDate, c.CreatedAt
        FROM dbo.Companies c
        WHERE @HasRestriction = 0 OR c.CompanyId IN (SELECT CompanyId FROM @CompanyIds)
    ),
    CompanyAgg AS (
        SELECT
            COUNT(*) AS TotalCompanies,
            ISNULL(SUM(CASE WHEN IsActive = 1 AND PlanEndDate >= @Today AND PlanType = 'Trial' THEN 1 ELSE 0 END), 0) AS TrialCompanies,
            ISNULL(SUM(CASE WHEN IsActive = 1 AND PlanEndDate >= @Today AND PlanType = 'License' THEN 1 ELSE 0 END), 0) AS LicensedCompanies,
            ISNULL(SUM(CASE WHEN IsActive = 0 OR PlanEndDate < @Today THEN 1 ELSE 0 END), 0) AS InactiveCompanies,
            ISNULL(SUM(CASE WHEN IsActive = 1 AND PlanType = 'License' AND PlanEndDate BETWEEN @Today AND @ThirtyDaysOut THEN 1 ELSE 0 END), 0) AS ExpiringLicensesIn30Days,
            ISNULL(SUM(CASE
                WHEN @StartDate IS NULL AND @EndDate IS NULL THEN 0
                WHEN (@StartDate IS NULL OR CreatedAt >= @StartDate) AND (@EndDate IS NULL OR CreatedAt < DATEADD(DAY, 1, @EndDate)) THEN 1
                ELSE 0 END), 0) AS CompaniesAddedInPeriod
        FROM ScopedCompanies
    ),
    -- Only the 4 role names any dashboard bucket ever counts (CompanyAdmin / Manager+legacy-Admin / Student) -
    -- mirrors DashboardQueryService.ActiveRoleUserIdsQuery and its usersAddedInPeriod scope exactly.
    ScopedRoles AS (
        SELECT ucr.UserId, r.RoleName
        FROM dbo.UserCompanyRoles ucr
        JOIN dbo.Roles r ON r.RoleId = ucr.RoleId
        WHERE ucr.IsActive = 1
          AND (ucr.StartDate IS NULL OR ucr.StartDate <= @Today)
          AND (ucr.EndDate IS NULL OR ucr.EndDate >= @Today)
          AND r.RoleName IN ('CompanyAdmin', 'Manager', 'Admin', 'Student')
          AND (@HasRestriction = 0 OR ucr.CompanyId IN (SELECT CompanyId FROM @CompanyIds))
    ),
    RoleAgg AS (
        SELECT
            COUNT(DISTINCT CASE WHEN RoleName = 'CompanyAdmin' THEN UserId END) AS TotalCompanyAdmins,
            COUNT(DISTINCT CASE WHEN RoleName IN ('Manager', 'Admin') THEN UserId END) AS TotalManagers,
            COUNT(DISTINCT CASE WHEN RoleName = 'Student' THEN UserId END) AS TotalEmployees
        FROM ScopedRoles
    ),
    EmployeeIds AS (
        SELECT DISTINCT UserId FROM ScopedRoles WHERE RoleName = 'Student'
    ),
    ItSplit AS (
        SELECT ISNULL(SUM(CASE WHEN sp.StudentType = 'IT' THEN 1 ELSE 0 END), 0) AS ItEmployees
        FROM dbo.StudentProfiles sp
        JOIN EmployeeIds e ON e.UserId = sp.UserId
    ),
    ScopedPersonIds AS (
        SELECT DISTINCT UserId FROM ScopedRoles
    ),
    UserAgg AS (
        SELECT ISNULL(SUM(CASE
            WHEN @StartDate IS NULL AND @EndDate IS NULL THEN 0
            WHEN (@StartDate IS NULL OR u.CreatedAt >= @StartDate) AND (@EndDate IS NULL OR u.CreatedAt < DATEADD(DAY, 1, @EndDate)) THEN 1
            ELSE 0 END), 0) AS UsersAddedInPeriod
        FROM dbo.Users u
        JOIN ScopedPersonIds p ON p.UserId = u.UserId
    ),
    -- ActiveLibraryCards only has a text Company_Code, not a CompanyId FK (see DashboardQueryService.ResolveCompanyCodesAsync) -
    -- only apply the code filter when a restriction is actually active, so an unrestricted call still counts
    -- legacy rows whose Company_Code has no matching Companies row, exactly like the LINQ version did.
    ScopedCards AS (
        SELECT al.Email, al.Company_Code AS CompanyCode, al.Start_Date AS StartDate
        FROM dbo.ActiveLibraryCards al
        WHERE al.Email IS NOT NULL
          AND (@HasRestriction = 0 OR al.Company_Code IN (SELECT CompanyCode FROM ScopedCompanies))
    ),
    CardAgg AS (
        SELECT COUNT(*) AS CourseLibraryUsers
        FROM (SELECT DISTINCT LOWER(Email) AS EmailLower, CompanyCode FROM ScopedCards) d
    ),
    CardPeriod AS (
        SELECT ISNULL(SUM(CASE
            WHEN @StartDate IS NULL AND @EndDate IS NULL THEN 0
            WHEN (@StartDate IS NULL OR StartDate >= @StartDate) AND (@EndDate IS NULL OR StartDate < DATEADD(DAY, 1, @EndDate)) THEN 1
            ELSE 0 END), 0) AS SessionsStartedInPeriod
        FROM ScopedCards
    )
    SELECT
        ca.TotalCompanies,
        ra.TotalCompanyAdmins,
        ra.TotalManagers,
        ra.TotalEmployees,
        ca.TrialCompanies,
        ca.LicensedCompanies,
        ca.InactiveCompanies,
        ca.ExpiringLicensesIn30Days,
        isplit.ItEmployees,
        ra.TotalEmployees - isplit.ItEmployees AS NonItEmployees,
        cardagg.CourseLibraryUsers,
        ca.CompaniesAddedInPeriod,
        useragg.UsersAddedInPeriod,
        cardperiod.SessionsStartedInPeriod AS CourseLibrarySessionsStartedInPeriod
    FROM CompanyAgg ca
    CROSS JOIN RoleAgg ra
    CROSS JOIN ItSplit isplit
    CROSS JOIN CardAgg cardagg
    CROSS JOIN UserAgg useragg
    CROSS JOIN CardPeriod cardperiod
    OPTION (RECOMPILE);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_GetDashboardStats");
        }
    }
}
