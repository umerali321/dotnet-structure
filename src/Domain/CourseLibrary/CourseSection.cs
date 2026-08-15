namespace SkillsetsBackend.Domain.CourseLibrary;

/// <summary>Maps to the existing "CourseSections" table (the course's table-of-contents). Read-only.</summary>
public class CourseSection
{
    public long SectionId { get; private set; }

    public long CourseId { get; private set; }

    public string SectionName { get; private set; } = string.Empty;

    public string? SectionUrl { get; private set; }

    public string? Duration { get; private set; }

    public string? Status { get; private set; }

    public int? DisplayOrder { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private CourseSection()
    {
    }
}
