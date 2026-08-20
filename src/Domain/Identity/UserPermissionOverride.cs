namespace SkillsetsBackend.Domain.Identity;

/// <summary>
/// A per-user delta on top of their role's default permissions - composite key (UserId,
/// PermissionId). IsGranted=true means this specific person has this permission even though their
/// role doesn't grant it; IsGranted=false means this specific person is denied it even though their
/// role does grant it. Absence of a row means "just use the role's default" - see
/// IPermissionService.GetEffectivePermissionKeysForUserAsync for how these merge with RolePermissions.
/// </summary>
public class UserPermissionOverride
{
    public int UserId { get; private set; }

    public int PermissionId { get; private set; }

    public bool IsGranted { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public string? UpdatedBy { get; private set; }

    private UserPermissionOverride()
    {
    }

    public UserPermissionOverride(int userId, int permissionId, bool isGranted, string? updatedBy)
    {
        UserId = userId;
        PermissionId = permissionId;
        IsGranted = isGranted;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
