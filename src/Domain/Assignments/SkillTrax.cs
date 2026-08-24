using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.Assignments;

/// <summary>A manager/company-admin-built, reusable bundle of course titles. Course membership
/// (see SkillTraxCourse) and the name can both be edited freely after creation - safe to do so
/// unconditionally because AssignmentTitle rows always store a CourseId snapshot directly, never a
/// live pointer through this bundle, so editing or even deleting a SkillTrax never touches any
/// assignment that was already created from it. "Delete" is a soft delete (IsDeleted/DeletedAt) -
/// the row is kept for audit/history purposes and simply excluded from every normal query via the
/// EF global query filter on this entity; it is never physically removed.</summary>
public class SkillTrax : IAggregateRoot
{
    public int SkillTraxId { get; private set; }

    public int CreatedByUserId { get; private set; }

    public int CompanyId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    private SkillTrax()
    {
    }

    public static SkillTrax Create(int createdByUserId, int companyId, string name)
    {
        return new SkillTrax
        {
            CreatedByUserId = createdByUserId,
            CompanyId = companyId,
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Rename(string name)
    {
        Name = name;
    }

    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
    }
}
