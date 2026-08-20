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
    int ItEmployees,
    int NonItEmployees,
    int CourseLibraryUsers,
    int CompaniesAddedInPeriod,
    int UsersAddedInPeriod,
    int CourseLibrarySessionsStartedInPeriod);
