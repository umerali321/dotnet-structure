namespace SkillsetsBackend.Domain.Assignments;

/// <summary>One course title inside one Assignment. Composite key (AssignmentId, CourseId) - a
/// snapshot taken at assignment creation time (from either the single selected course or the
/// SkillTrax's SkillTraxCourses at that moment), independent of whatever the source SkillTrax
/// contains later or whether it still exists.</summary>
public class AssignmentTitle
{
    public int AssignmentId { get; private set; }

    public long CourseId { get; private set; }

    private AssignmentTitle()
    {
    }

    public AssignmentTitle(int assignmentId, long courseId)
    {
        AssignmentId = assignmentId;
        CourseId = courseId;
    }
}
