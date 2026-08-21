namespace SkillsetsBackend.Application.CourseLibrary.Queries.GetCourseLibrary;

/// <summary>Type is the raw client-supplied value ("non-it" or "it") - the handler maps it
/// to MainCourseCategories.TypeID rather than the controller/query hard-coding the ID.</summary>
public record GetCourseLibraryQuery(string Type);
