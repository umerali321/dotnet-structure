namespace SkillsetsBackend.Domain.Assignments;

/// <summary>One employee targeted by one Assignment. Composite key (AssignmentId, StudentUserId) -
/// an assignment can target one or many employees at once.</summary>
public class AssignmentEmployee
{
    public int AssignmentId { get; private set; }

    public int StudentUserId { get; private set; }

    private AssignmentEmployee()
    {
    }

    public AssignmentEmployee(int assignmentId, int studentUserId)
    {
        AssignmentId = assignmentId;
        StudentUserId = studentUserId;
    }
}
