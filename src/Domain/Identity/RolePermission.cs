namespace SkillsetsBackend.Domain.Identity;

/// <summary>
/// Join row granting one Permission to one Role - composite key (RoleId, PermissionId), no
/// surrogate id. This is what SuperAdmin actually edits when configuring a custom role.
/// </summary>
public class RolePermission
{
    public byte RoleId { get; private set; }

    public int PermissionId { get; private set; }

    private RolePermission()
    {
    }

    public RolePermission(byte roleId, int permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }
}
