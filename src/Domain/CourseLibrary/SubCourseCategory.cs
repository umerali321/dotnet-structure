namespace SkillsetsBackend.Domain.CourseLibrary;

/// <summary>Maps to the "SubCourseCategories" table (renamed from "SubCategories"). Read-only - not
/// yet surfaced in the Course Library UI (parent-category-only display for now), but mapped so
/// Courses.SubCategoryId can be resolved later without a schema change.</summary>
public class SubCourseCategory
{
    public int SubCategoryId { get; private set; }

    public int CategoryId { get; private set; }

    public string SubCategoryName { get; private set; } = string.Empty;

    public string? SubCategoryUrl { get; private set; }

    public int? DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private SubCourseCategory()
    {
    }
}
