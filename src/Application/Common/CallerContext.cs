using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Common;

/// <summary>The authenticated caller's identity, extracted from JWT claims by the controller.</summary>
public record CallerContext(string UserId, string Email, string Role)
{
    public bool IsSuperAdmin => Role == Roles.SuperAdmin;

    /// <summary>Null for the config-based SuperAdmin (whose subject id is a GUID, not a Users.UserId).</summary>
    public int? DbUserId => int.TryParse(UserId, out var id) ? id : null;
}
