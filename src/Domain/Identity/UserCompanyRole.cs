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

    /// <summary>How THIS role grant was made - see <see cref="Identity.CreationSource"/>. Separate
    /// from AppUser.CreationSource because the two genuinely differ: someone created by hand as an
    /// Employee can later be granted Manager by a roster import, and only this column can say so.
    /// </summary>
    public string CreationSource { get; private set; } = Identity.CreationSource.Manual;

    public DateTimeOffset CreatedAt { get; private set; }

    public Company Company { get; private set; } = null!;

    public Role Role { get; private set; } = null!;

    private UserCompanyRole()
    {
    }

    public UserCompanyRole(int userId, int companyId, byte roleId, DateOnly? startDate,
        string creationSource = Identity.CreationSource.Manual)
    {
        UserId = userId;
        CompanyId = companyId;
        RoleId = roleId;
        StartDate = startDate;
        IsActive = true;
        CreationSource = creationSource;
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
    public void Reactivate(DateOnly? startDate, string? creationSource = null)
    {
        IsActive = true;
        StartDate = startDate;
        EndDate = null;
        // Re-granting is a new grant as far as reporting is concerned, so the caller may restamp the
        // source. Left unchanged when omitted, so existing callers keep the original attribution.
        if (creationSource is not null)
        {
            CreationSource = creationSource;
        }
    }
}
