using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.CourseLibrary;

/// <summary>Brand-new table - tracks which course a student has launched from the Course Library.
/// Exclusivity (one active course per student, one active taker per course) is enforced both here
/// and, as the real safety net, via filtered unique indexes at the DB level - see
/// CourseTakenConfiguration.</summary>
public class CourseTaken : IAggregateRoot
{
    public int CourseTakenId { get; private set; }

    public int UserId { get; private set; }

    public long CourseId { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset AccessedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private CourseTaken()
    {
    }

    public static CourseTaken Create(int userId, long courseId)
    {
        var now = DateTimeOffset.UtcNow;
        return new CourseTaken
        {
            UserId = userId,
            CourseId = courseId,
            IsActive = true,
            AccessedAt = now,
            CreatedAt = now,
        };
    }

    public void MarkComplete()
    {
        IsActive = false;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
