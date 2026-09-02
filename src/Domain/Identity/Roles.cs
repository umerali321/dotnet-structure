namespace SkillsetsBackend.Domain.Identity;

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";

    public const string Manager = "Manager";

    public const string Student = "Student";

    /// <summary>Company-scoped admin who can create/manage Managers for their own company.</summary>
    public const string CompanyAdmin = "CompanyAdmin";

    /// <summary>
    /// SuperAdmin's delegate in the admin application: sees every company like a SuperAdmin does,
    /// but - unlike SuperAdmin - holds no blanket bypass. Everything a SystemAdmin can do comes from
    /// the permissions granted to the role in Roles &amp; Permissions, so a SuperAdmin decides exactly
    /// how much of the system each one gets.
    ///
    /// A SystemAdmin still carries a UserCompanyRoles row because that is how every DB-backed
    /// account resolves its role at login; that company is only a carrier and is deliberately
    /// ignored when scoping data (see CallerContext.HasGlobalCompanyScope).
    /// </summary>
    public const string SystemAdmin = "SystemAdmin";

    /// <summary>
    /// Maps a raw Roles.RoleName value from the database to the app's effective role.
    /// Admin and Manager are treated as the same role. CompanyAdmin is distinct and passes through
    /// unchanged - do not collapse it into Manager here (see AGENTS.md - a similarly-named "Company
    /// Admin" concept existed before and was deliberately removed; this is a new, narrower role).
    /// </summary>
    public static string Normalize(string dbRoleName) => dbRoleName switch
    {
        "Admin" => Manager,
        "Manager" => Manager,
        "Student" => Student,
        _ => dbRoleName,
    };
}
