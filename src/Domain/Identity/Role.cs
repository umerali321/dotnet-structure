namespace SkillsetsBackend.Domain.Identity;

/// <summary>
/// Maps to the existing "Roles" lookup table. The 5 pre-existing rows (Student/Manager/FDM/
/// Admin/CompanyAdmin - see Roles.cs) are system roles and stay read-only; Create() is only for
/// SuperAdmin-authored custom roles added by the new Roles/Permissions feature. RoleId is a
/// tinyint IDENTITY column in the DB, so it's left unset here and populated by EF on insert.
/// </summary>
public class Role
{
    public byte RoleId { get; private set; }

    public string RoleName { get; private set; } = string.Empty;

    /// <summary>True for the 5 pre-existing rows whose behavior is still governed by hardcoded
    /// checks (StudentAuthorization.cs etc.) - custom roles created via Create() are false, and
    /// only custom roles' permissions are editable through the new Roles UI.</summary>
    public bool IsSystemRole { get; private set; }

    private Role()
    {
    }

    public static Role Create(string roleName) => new()
    {
        RoleName = roleName,
        IsSystemRole = false,
    };
}
