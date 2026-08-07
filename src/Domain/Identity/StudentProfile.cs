using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.Identity;

/// <summary>
/// Maps to the existing "StudentProfiles" table. CreatedBy/UpdatedBy/UpdatedAt are new, additive,
/// nullable columns (see the AddStudentProfileAuditColumns migration) - legacy rows predating them
/// are null and should be displayed as "Legacy Record", never backfilled.
/// </summary>
public class StudentProfile : IAggregateRoot
{
    public int StudentProfileId { get; private set; }

    public int UserId { get; private set; }

    public string? StudentType { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public string? CreatedBy { get; private set; }

    public string? UpdatedBy { get; private set; }

    private StudentProfile()
    {
    }

    public StudentProfile(int userId, string? studentType, string createdBy)
    {
        UserId = userId;
        StudentType = studentType;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public void Update(string? studentType, string updatedBy)
    {
        StudentType = studentType;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
