using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Common;

/// <summary>The authenticated caller's identity, extracted from JWT claims by the controller.</summary>
public record CallerContext(string UserId, string Email, string Role)
{
    /// <summary>The single built-in owner account. Use this ONLY where SuperAdmin must be
    /// distinguished from a SystemAdmin - i.e. managing System Administrators themselves. For an
    /// ordinary "is this an unrestricted platform administrator" check use <see cref="IsPlatformAdmin"/>,
    /// otherwise the feature silently locks SystemAdmins out.</summary>
    public bool IsSuperAdmin => Role == Roles.SuperAdmin;

    public bool IsSystemAdmin => Role == Roles.SystemAdmin;

    /// <summary>An unrestricted platform administrator: SuperAdmin, or a SystemAdmin, which the
    /// product defines as having the same reach ("system admin have all system access, all super
    /// admin level same access"). Bypasses permission checks and hardcoded role gates alike.
    ///
    /// The one thing this deliberately does NOT cover is administering System Administrators
    /// (creating one, resetting one's password) - that stays SuperAdmin-only via
    /// SystemAdminAuthorization, so a SystemAdmin cannot mint peers or take over another's account.</summary>
    public bool IsPlatformAdmin => IsSuperAdmin || IsSystemAdmin;

    /// <summary>Sees every company's data rather than being narrowed to their own memberships. The
    /// company on a SystemAdmin's UserCompanyRoles row is only a carrier so login can resolve a
    /// role; they administer all companies, not that one.</summary>
    public bool HasGlobalCompanyScope => IsPlatformAdmin;

    /// <summary>Null for the config-based SuperAdmin (whose subject id is a GUID, not a Users.UserId).</summary>
    public int? DbUserId => int.TryParse(UserId, out var id) ? id : null;
}
