namespace SkillsetsBackend.Application.Dashboard.Dtos;

/// <summary>
/// "Snapshot" fields (everything except the *InPeriod ones) reflect current state, scoped only by
/// company - they are not affected by the date-range filter (a "Trial Companies" count for a date
/// range would be meaningless). The *InPeriod fields are the only ones the date-range filter changes.
/// </summary>
public record DashboardStatsDto(
    int TotalCompanies,
    int TotalCompanyAdmins,
    int TotalManagers,
    int TotalEmployees,
    int TrialCompanies,
    int LicensedCompanies,
    int InactiveCompanies,
    /// <summary>Currently-licensed, active companies whose PlanEndDate falls within the next 30
    /// days - a subset of LicensedCompanies, surfaced separately so admins can see renewals coming
    /// due without it changing what "Valid Licensed" itself counts.</summary>
    int ExpiringLicensesIn30Days,
    int ItEmployees,
    int NonItEmployees,
    int CourseLibraryUsers,
    int CompaniesAddedInPeriod,
    int UsersAddedInPeriod,
    int CourseLibrarySessionsStartedInPeriod,
    /// <summary>Every course a scoped employee has ever taken, active or completed - a plain count
    /// of CourseTakens rows (not a distinct-course count), narrowed to a Manager's own team when
    /// restrictToManagerId is set, company-wide otherwise.</summary>
    int TotalCourseUsage);
