namespace SkillsetsBackend.Domain.Identity;

/// <summary>Maps to the existing "Roles" lookup table. Read-only.</summary>
public class Role
{
    public byte RoleId { get; private set; }

    public string RoleName { get; private set; } = string.Empty;

    private Role()
    {
    }
}
