namespace SkillsetsBackend.Application.Dashboard.Dtos;

/// <summary>
/// One row per distinct (Email, CompanyCode) pair in ActiveLibraryCards - a person can have several
/// underlying card rows (each a start/renewal), collapsed here into one summary row with a session
/// count. Name/StudentType/UserId come from the matched registered Users/StudentProfiles record when
/// one exists (matched by lower-cased email, same rule as ActiveLibraryCardResolver); otherwise they
/// fall back to the card's own legacy First/Last Name and StudentType is left null.
/// </summary>
public record CourseLibraryUserDto(
    string Email,
    string FullName,
    int? UserId,
    string CompanyName,
    int? CompanyId,
    string? StudentType,
    DateTime LatestSessionStart,
    DateTime LatestSessionEnd,
    string SessionStatus,
    int SessionCount);
