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

    /// <summary>Re-activates a previously-deactivated row for the same (UserId, CompanyId, RoleId)
    /// triple, rather than inserting a new one - UX_UserCompanyRoles_User_Company_Role is a real,
    /// unfiltered unique index on that triple in the underlying legacy table (it does not exempt
    /// inactive rows), so a second insert for a role this person already held (even long since
    /// revoked) at this company would violate it.</summary>
    public void Reactivate(DateOnly? startDate)
    {
        IsActive = true;
        StartDate = startDate;
        EndDate = null;
    }
}
