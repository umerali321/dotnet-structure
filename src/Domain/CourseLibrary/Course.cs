namespace SkillsetsBackend.Domain.CourseLibrary;

/// <summary>Maps to the existing "Courses" table (scraped Skillsoft catalog content). Read-only.</summary>
public class Course
{
    public long CourseId { get; private set; }

    public int CategoryId { get; private set; }

    public int? SubCategoryId { get; private set; }

    public string CourseTitle { get; private set; } = string.Empty;

    public string? CourseUrl { get; private set; }

    public string? LaunchUrl { get; private set; }

    public string? Duration { get; private set; }

    public string? ExpertiseLevel { get; private set; }

    public string? Status { get; private set; }

    public string? AboutContent { get; private set; }

    public string? OverviewContent { get; private set; }

    public string? ImageUrl { get; private set; }

    public string? SkillsoftCourseCode { get; private set; }

    public int? DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset ScrapedAt { get; private set; }

    public string? CourseSourceUrl { get; private set; }

    public string? SourceStatus { get; private set; }

    private Course()
    {
    }
}
