using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.Assignments;

/// <summary>One "Assign Training" action: a single course or a SkillTrax bundle, assigned to one or
/// more employees (see AssignmentEmployee) for a 30-day Focus Session window. AssignmentTitles
/// stores a snapshot of the assigned CourseIds at creation time - it never depends on SourceSkillTrax
/// still existing, so deleting a SkillTrax never erases historical assignment data.
///
/// Deliberately has no computed Status - usage-driven completion tracking (test-score-based
/// completion, Scheduled/InProgress/PastDue/etc.) depends on Skillport usage integration that
/// doesn't exist yet and is out of scope for this pass. IsCancelled/CancelledAt is the only
/// lifecycle state tracked here.</summary>
public class Assignment : IAggregateRoot
{
    private const int FocusSessionDays = 30;

    public int AssignmentId { get; private set; }

    public int CreatedByUserId { get; private set; }

    public int CompanyId { get; private set; }

    public AssignmentSourceType SourceType { get; private set; }

    public int? SourceSkillTraxId { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public bool IsCancelled { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public int? UpdatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    private Assignment()
    {
    }

    public static Assignment Create(
        int createdByUserId, int companyId, AssignmentSourceType sourceType, int? sourceSkillTraxId, DateOnly startDate)
    {
        return new Assignment
        {
            CreatedByUserId = createdByUserId,
            CompanyId = companyId,
            SourceType = sourceType,
            SourceSkillTraxId = sourceSkillTraxId,
            StartDate = startDate,
            EndDate = startDate.AddDays(FocusSessionDays),
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Cancel()
    {
        IsCancelled = true;
        CancelledAt = DateTimeOffset.UtcNow;
    }

    public void UpdateStartDate(DateOnly startDate)
    {
        StartDate = startDate;
        EndDate = startDate.AddDays(FocusSessionDays);
    }

    /// <summary>Called on every edit (employees, start date, and/or titles) - null for a SuperAdmin
    /// caller, which has no DbUserId of its own (see AGENTS.md). Not called by Cancel(), which
    /// already has its own CancelledAt/IsCancelled lifecycle tracking.</summary>
    public void MarkUpdated(int? updatedByUserId)
    {
        UpdatedByUserId = updatedByUserId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsActiveOrScheduled(DateOnly today) => !IsCancelled && EndDate >= today;
}
