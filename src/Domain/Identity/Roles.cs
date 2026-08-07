namespace SkillsetsBackend.Domain.Identity;

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";

    public const string Manager = "Manager";

    public const string Student = "Student";

    /// <summary>
    /// Maps a raw Roles.RoleName value from the database to the app's effective role.
    /// Admin and Manager are treated as the same role.
    /// </summary>
    public static string Normalize(string dbRoleName) => dbRoleName switch
    {
        "Admin" => Manager,
        "Manager" => Manager,
        "Student" => Student,
        _ => dbRoleName,
    };
}
