using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.CourseLibrary;

/// <summary>Tracks which course a student has launched from the Course Library. Exclusivity is
/// per-student only (one active course per student, matching the 30-day Focus Session model) -
/// enforced both here and, as the real safety net, via a filtered unique index at the DB level, see
/// CourseTakenConfiguration. There is deliberately no per-course exclusivity: the same course (e.g.
/// a standardized compliance title) must be startable by many students across many companies at
/// once - an earlier version of this schema had a global "one active taker per course" unique index,
/// which was a bug (it let one student's abandoned, never-completed session permanently block every
/// other student on the platform from ever starting that course).</summary>
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
