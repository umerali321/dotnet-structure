using SkillsetsBackend.Application.Common;

namespace SkillsetsBackend.Application.SystemAdmins;

/// <summary>
/// Managing SystemAdmins is SuperAdmin-only, and deliberately hardcoded rather than driven by a
/// permission.
///
/// A permission would be grantable, and the first thing a SuperAdmin might reasonably grant a
/// SystemAdmin is "manage system admins" - at which point that SystemAdmin can create peers, or
/// hand itself a role with more permissions, and the ceiling a SuperAdmin thought they had set is
/// gone. Keeping this outside the permission system means the only way to widen a SystemAdmin's
/// reach is for a SuperAdmin to do it deliberately.
/// </summary>
public static class SystemAdminAuthorization
{
    public static void EnsureSuperAdmin(CallerContext caller)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only a SuperAdmin can manage System Administrators.");
        }
    }
}
