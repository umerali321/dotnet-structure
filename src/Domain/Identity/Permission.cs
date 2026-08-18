namespace SkillsetsBackend.Domain.Identity;

/// <summary>
/// The permission catalog - a brand-new table, seeded once via migration and never written to
/// through any API (SuperAdmin assigns existing permissions to roles; nobody creates new
/// permission rows at runtime). No public constructor on purpose - only migration seed data
/// produces these.
/// </summary>
public class Permission
{
    public int PermissionId { get; private set; }

    /// <summary>Stable machine key checked in code, e.g. "Students.View", "Roles.Manage".</summary>
    public string PermissionKey { get; private set; } = string.Empty;

    /// <summary>Groups permissions for display, e.g. "Students", "Managers", "Roles".</summary>
    public string Category { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    private Permission()
    {
    }
}
