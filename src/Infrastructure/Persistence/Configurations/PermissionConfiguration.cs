using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(x => x.PermissionId);

        builder.Property(x => x.PermissionKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(300).IsRequired();

        builder.HasIndex(x => x.PermissionKey).IsUnique();

        // Seeded once via migration, never created through any API - see Permissions.cs (Domain)
        // for the key constants these mirror. HasData needs plain property-matching objects since
        // Permission has no public constructor.
        builder.HasData(
            Seed(Permissions.Students.ViewId, Permissions.Students.View, "Employees", "View Employees"),
            Seed(Permissions.Students.CreateId, Permissions.Students.Create, "Employees", "Create Employees"),
            Seed(Permissions.Students.UpdateId, Permissions.Students.Update, "Employees", "Edit Employees"),
            Seed(Permissions.Students.DeleteId, Permissions.Students.Delete, "Employees", "Delete Employees"),
            Seed(Permissions.Students.ManagePasswordId, Permissions.Students.ManagePassword, "Employees", "Reset Employee Password"),
            Seed(Permissions.Students.AssignManagerId, Permissions.Students.AssignManager, "Employees", "Assign Employee to a Manager"),
            Seed(Permissions.Students.ViewCredentialsId, Permissions.Students.ViewCredentials, "Employees", "View Employee Login Credentials"),

            Seed(Permissions.Managers.ViewId, Permissions.Managers.View, "Managers", "View Managers"),
            Seed(Permissions.Managers.CreateId, Permissions.Managers.Create, "Managers", "Create Managers"),
            Seed(Permissions.Managers.UpdateId, Permissions.Managers.Update, "Managers", "Edit Managers"),
            Seed(Permissions.Managers.ManagePasswordId, Permissions.Managers.ManagePassword, "Managers", "Reset Manager Password"),
            Seed(Permissions.Managers.ViewCredentialsId, Permissions.Managers.ViewCredentials, "Managers", "View Manager Login Credentials"),

            Seed(Permissions.Companies.ViewId, Permissions.Companies.View, "Companies", "View Companies"),
            Seed(Permissions.Companies.CreateId, Permissions.Companies.Create, "Companies", "Create Companies"),

            Seed(Permissions.Roles.ViewId, Permissions.Roles.View, "Roles & Permissions", "View Roles & Permissions"),
            Seed(Permissions.Roles.ManageId, Permissions.Roles.Manage, "Roles & Permissions", "Create & Edit Roles"),

            Seed(Permissions.Faq.ViewId, Permissions.Faq.View, "FAQs", "View FAQs"),
            Seed(Permissions.Faq.CreateId, Permissions.Faq.Create, "FAQs", "Create FAQs"),
            Seed(Permissions.Faq.UpdateId, Permissions.Faq.Update, "FAQs", "Edit FAQs"),
            Seed(Permissions.Faq.DeleteId, Permissions.Faq.Delete, "FAQs", "Delete FAQs"),

            Seed(Permissions.ContactInfo.ViewId, Permissions.ContactInfo.View, "Contact Info", "View Contact Info"),
            Seed(Permissions.ContactInfo.CreateId, Permissions.ContactInfo.Create, "Contact Info", "Create Contact Info"),
            Seed(Permissions.ContactInfo.UpdateId, Permissions.ContactInfo.Update, "Contact Info", "Edit Contact Info"),
            Seed(Permissions.ContactInfo.DeleteId, Permissions.ContactInfo.Delete, "Contact Info", "Delete Contact Info"),

            Seed(Permissions.SystemLogs.ViewId, Permissions.SystemLogs.View, "System Logs", "View System Logs"),

            Seed(Permissions.CourseLibrary.ViewId, Permissions.CourseLibrary.View, "Course Library", "View Course Library"),
            Seed(Permissions.CourseLibrary.PublishId, Permissions.CourseLibrary.Publish, "Course Library", "Publish Courses"),

            Seed(Permissions.Skillsoft.LaunchCourseId, Permissions.Skillsoft.LaunchCourse, "Course Provider", "Launch Courses"),
            Seed(Permissions.Skillsoft.ViewTranscriptId, Permissions.Skillsoft.ViewTranscript, "Course Provider", "View Learning Transcript"),
            Seed(Permissions.Skillsoft.ViewCatalogId, Permissions.Skillsoft.ViewCatalog, "Course Provider", "View Course Catalog"),

            Seed(Permissions.Assignments.ViewId, Permissions.Assignments.View, "Assignments", "View Training Assignments"),
            Seed(Permissions.Assignments.CreateId, Permissions.Assignments.Create, "Assignments", "Assign Training"),
            Seed(Permissions.Assignments.CancelId, Permissions.Assignments.Cancel, "Assignments", "Cancel Training Assignments"),

            Seed(Permissions.SkillTrax.ViewId, Permissions.SkillTrax.View, "SkillTrax", "View SkillTrax"),
            Seed(Permissions.SkillTrax.CreateId, Permissions.SkillTrax.Create, "SkillTrax", "Create SkillTrax"),
            Seed(Permissions.SkillTrax.DeleteId, Permissions.SkillTrax.Delete, "SkillTrax", "Delete SkillTrax"),

            Seed(Permissions.LearningTranscript.ViewId, Permissions.LearningTranscript.View, "Learning Transcript", "View My Learning Transcript"),
            Seed(Permissions.LearningTranscript.ViewReportId, Permissions.LearningTranscript.ViewReport, "Learning Transcript", "View Learning Transcript Report"),
            Seed(Permissions.LearningTranscript.ViewEmployeeTranscriptId, Permissions.LearningTranscript.ViewEmployeeTranscript, "Learning Transcript", "View an Employee's Transcript"),
            Seed(Permissions.LearningTranscript.ExportId, Permissions.LearningTranscript.Export, "Learning Transcript", "Export Learning Transcript Report"),
            Seed(Permissions.LearningTranscript.ImportId, Permissions.LearningTranscript.Import, "Learning Transcript", "Import Learning Transcript Data")
        );
    }

    private static object Seed(int permissionId, string permissionKey, string category, string description) => new
    {
        PermissionId = permissionId,
        PermissionKey = permissionKey,
        Category = category,
        Description = description,
    };
}
