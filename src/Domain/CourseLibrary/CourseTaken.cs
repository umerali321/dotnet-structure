using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.CourseLibrary;

/// <summary>Tracks which course a student has launched from the Course Library.
///
/// There is NO exclusivity of any kind. A student may have any number of courses in progress at
/// once, and a course may be in progress for any number of students.
///
/// Both restrictions were removed for the same underlying reason - each let one record block work
/// that should have been allowed:
///   - "one active taker per course" (removed earlier): one student's abandoned session permanently
///     blocked every other student on the platform from ever starting that course.
///   - "one active course per student" (removed at the customer's request): completion is derived
///     from the Skillport usage report, which can lag by up to two days, so a course the student had
///     genuinely finished still read "In Progress" and held them up. Nothing about the 30-day
///     session was ever meant to cap how many courses run at once.
/// </summary>
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

    /// <summary>Abandons this course so the student can start a different one - deliberately does
    /// NOT set CompletedAt, since the course was given up rather than finished. Only the imported
    /// Skillport transcript records a genuine completion, so claiming one here would put a
    /// completion into the record that Skillport never reported.</summary>
    public void Cancel()
    {
        IsActive = false;
    }
}
