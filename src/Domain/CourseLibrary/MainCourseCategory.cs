namespace SkillsetsBackend.Domain.CourseLibrary;

/// <summary>Maps to the "MainCourseCategories" table (renamed from "LibraryCategories"). Read-only -
/// TypeID distinguishes NON-IT (1) from IT (2) categories; do not hard-code that mapping anywhere else.</summary>
public class MainCourseCategory
{
    public int CategoryId { get; private set; }

    public int TypeId { get; private set; }

    public string CategoryName { get; private set; } = string.Empty;

    public string? CategoryUrl { get; private set; }

    public int? DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private MainCourseCategory()
    {
    }
}
