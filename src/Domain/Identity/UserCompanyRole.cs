namespace SkillsetsBackend.Domain.Identity;

/// <summary>Maps to the existing "UserCompanyRoles" table. Read-only for now.</summary>
public class UserCompanyRole
{
    public int UserCompanyRoleId { get; private set; }

    public int UserId { get; private set; }

    public int CompanyId { get; private set; }

    public byte RoleId { get; private set; }

    public DateOnly? StartDate { get; private set; }

    public DateOnly? EndDate { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Company Company { get; private set; } = null!;

    public Role Role { get; private set; } = null!;

    private UserCompanyRole()
    {
    }
}
