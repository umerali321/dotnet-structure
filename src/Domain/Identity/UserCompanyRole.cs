namespace SkillsetsBackend.Domain.Identity;

/// <summary>Maps to the existing "UserCompanyRoles" table.</summary>
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

    public UserCompanyRole(int userId, int companyId, byte roleId, DateOnly? startDate)
    {
        UserId = userId;
        CompanyId = companyId;
        RoleId = roleId;
        StartDate = startDate;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate(DateOnly? endDate)
    {
        IsActive = false;
        EndDate = endDate;
    }
}
