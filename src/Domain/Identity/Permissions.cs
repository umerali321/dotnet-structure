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

        /// <summary>Bulk Employee Roster Import. Grantable, so a Manager or Company Admin can be
        /// allowed to import their own roster without being handed anything else.</summary>
        public const int ImportId = 8;
        public const string Import = "Students.Import";
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

        /// <summary>Editing an existing company: details, logo, licence, activate/deactivate. These
        /// actions previously had no key at all and were hardcoded to SuperAdmin, so they could not
        /// be delegated to anyone.</summary>
        public const int ManageId = 23;
        public const string Manage = "Companies.Manage";
    }

    public static class Roles
    {
        public const int ViewId = 31;
        public const string View = "Roles.View";

        public const int ManageId = 32;
        public const string Manage = "Roles.Manage";

        /// <summary>Granting or revoking a ROLE on another user (the role-assignment dialog on the
        /// Manager/Employee screens) - distinct from Manage, which is about editing the roles
        /// themselves and their permission sets. Holding this shows the assign icon and allows the
        /// change; without it the icon is hidden and the API refuses.</summary>
        public const int AssignId = 33;
        public const string Assign = "Roles.Assign";
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

    public static class Assignments
    {
        public const int ViewId = 91;
        public const string View = "Assignments.View";

        public const int CreateId = 92;
        public const string Create = "Assignments.Create";

        public const int CancelId = 93;
        public const string Cancel = "Assignments.Cancel";
    }

    public static class SkillTrax
    {
        public const int ViewId = 101;
        public const string View = "SkillTrax.View";

        public const int CreateId = 102;
        public const string Create = "SkillTrax.Create";

        public const int DeleteId = 103;
        public const string Delete = "SkillTrax.Delete";
    }

    public static class LearningTranscript
    {
        public const int ViewId = 111;
        public const string View = "LearningTranscript.View";

        public const int ViewReportId = 112;
        public const string ViewReport = "LearningTranscript.ViewReport";

        public const int ViewEmployeeTranscriptId = 113;
        public const string ViewEmployeeTranscript = "LearningTranscript.ViewEmployeeTranscript";

        public const int ExportId = 114;
        public const string Export = "LearningTranscript.Export";

        public const int ImportId = 115;
        public const string Import = "LearningTranscript.Import";
    }

    /// <summary>
    /// The Settings area. View is the gate on the section itself - without it none of the settings
    /// pages are reachable at all; the rest control the individual pages, so a role can be given,
    /// say, Email History without also being handed the SMTP credentials screen.
    ///
    /// Roles/FAQ/Contact Info pages inside Settings are deliberately NOT re-declared here - they
    /// already have their own permissions (Roles.*, Faq.*, ContactInfo.*) and adding a second key
    /// for the same screen would mean two switches that disagree.
    /// </summary>
    public static class Settings
    {
        public const int ViewId = 121;
        public const string View = "Settings.View";

        public const int ManageEmailId = 122;
        public const string ManageEmail = "Settings.ManageEmail";

        public const int ViewEmailHistoryId = 123;
        public const string ViewEmailHistory = "Settings.ViewEmailHistory";

        public const int ManageNotificationsId = 124;
        public const string ManageNotifications = "Settings.ManageNotifications";

        public const int ManageScraperId = 125;
        public const string ManageScraper = "Settings.ManageScraper";

        public const int ManageAppSettingsId = 126;
        public const string ManageAppSettings = "Settings.ManageAppSettings";
    }
}
