namespace SkillsetsBackend.Domain.Identity;

/// <summary>
/// The full permission key catalog, matching the seeded "Permissions" table exactly (see
/// PermissionConfiguration.cs). Use these constants everywhere a permission is checked or
/// assigned - never a raw string - so a typo can't silently create a permission key nothing was
/// ever seeded for. IDs are stable and hand-assigned in blocks per category so a new permission
/// added later doesn't renumber existing rows (which would break already-seeded RolePermissions).
/// </summary>
public static class Permissions
{
    public static class Students
    {
        public const int ViewId = 1;
        public const string View = "Students.View";

        public const int CreateId = 2;
        public const string Create = "Students.Create";

        public const int UpdateId = 3;
        public const string Update = "Students.Update";

        public const int DeleteId = 4;
        public const string Delete = "Students.Delete";

        public const int ManagePasswordId = 5;
        public const string ManagePassword = "Students.ManagePassword";

        public const int AssignManagerId = 6;
        public const string AssignManager = "Students.AssignManager";

        public const int ViewCredentialsId = 7;
        public const string ViewCredentials = "Students.ViewCredentials";
    }

    public static class Managers
    {
        public const int ViewId = 11;
        public const string View = "Managers.View";

        public const int CreateId = 12;
        public const string Create = "Managers.Create";

        public const int UpdateId = 13;
        public const string Update = "Managers.Update";

        public const int ManagePasswordId = 14;
        public const string ManagePassword = "Managers.ManagePassword";

        public const int ViewCredentialsId = 15;
        public const string ViewCredentials = "Managers.ViewCredentials";
    }

    public static class Companies
    {
        public const int ViewId = 21;
        public const string View = "Companies.View";

        public const int CreateId = 22;
        public const string Create = "Companies.Create";
    }

    public static class Roles
    {
        public const int ViewId = 31;
        public const string View = "Roles.View";

        public const int ManageId = 32;
        public const string Manage = "Roles.Manage";
    }

    public static class Faq
    {
        public const int ViewId = 41;
        public const string View = "Faq.View";

        public const int CreateId = 42;
        public const string Create = "Faq.Create";

        public const int UpdateId = 43;
        public const string Update = "Faq.Update";

        public const int DeleteId = 44;
        public const string Delete = "Faq.Delete";
    }

    public static class ContactInfo
    {
        public const int ViewId = 51;
        public const string View = "ContactInfo.View";

        public const int CreateId = 52;
        public const string Create = "ContactInfo.Create";

        public const int UpdateId = 53;
        public const string Update = "ContactInfo.Update";

        public const int DeleteId = 54;
        public const string Delete = "ContactInfo.Delete";
    }

    public static class SystemLogs
    {
        public const int ViewId = 61;
        public const string View = "SystemLogs.View";
    }

    public static class CourseLibrary
    {
        public const int ViewId = 71;
        public const string View = "CourseLibrary.View";

        public const int PublishId = 72;
        public const string Publish = "CourseLibrary.Publish";
    }

    public static class Skillsoft
    {
        public const int LaunchCourseId = 81;
        public const string LaunchCourse = "Skillsoft.LaunchCourse";

        public const int ViewTranscriptId = 82;
        public const string ViewTranscript = "Skillsoft.ViewTranscript";

        public const int ViewCatalogId = 83;
        public const string ViewCatalog = "Skillsoft.ViewCatalog";
    }
}
