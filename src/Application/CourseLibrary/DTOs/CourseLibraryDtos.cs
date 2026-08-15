namespace SkillsetsBackend.Application.CourseLibrary.DTOs;

public record CourseLibrarySectionDto(
    long SectionId,
    string SectionName,
    string? Duration,
    string? Status,
    int? DisplayOrder);

/// <summary>Used by the category course listing - title, duration, expertise level, and a launch
/// URL for a quick-launch button right in the list. The heavier About content/Table of Contents
/// is fetched separately, on demand, only once the user opens a specific course.</summary>
public record CourseLibraryCourseSummaryDto(
    long CourseId,
    string CourseTitle,
    string? Duration,
    string? ExpertiseLevel,
    string? LaunchUrl);

public record CourseLibraryCourseDto(
    long CourseId,
    string CourseTitle,
    string? Duration,
    string? ExpertiseLevel,
    string? Status,
    string? CourseUrl,
    string? LaunchUrl,
    string? AboutContent,
    string? OverviewContent,
    string? ImageUrl,
    string? SkillsoftCourseCode,
    IReadOnlyList<CourseLibrarySectionDto> Sections);

public record CourseLibraryCategoryDto(
    int CategoryId,
    string CategoryName,
    IReadOnlyList<CourseLibraryCourseSummaryDto> Courses);

public record CourseLibraryResponseDto(
    string Type,
    IReadOnlyList<CourseLibraryCategoryDto> Categories);
