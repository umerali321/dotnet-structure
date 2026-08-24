namespace SkillsetsBackend.Domain.Assignments;

/// <summary>Membership row: one course title inside one SkillTrax bundle. Composite key
/// (SkillTraxId, CourseId). Set once when the SkillTrax is created - there is no Edit in the
/// initial release, so these rows are never modified after insert.</summary>
public class SkillTraxCourse
{
    public int SkillTraxId { get; private set; }

    public long CourseId { get; private set; }

    private SkillTraxCourse()
    {
    }

    public SkillTraxCourse(int skillTraxId, long courseId)
    {
        SkillTraxId = skillTraxId;
        CourseId = courseId;
    }
}
