using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    // RoleIds of the 5 pre-existing system rows (see the live Roles table / AGENTS.md).
    private const byte StudentRoleId = 1;
    private const byte ManagerRoleId = 2;
    private const byte FdmRoleId = 3; // dormant, unassigned to anyone - seeded with zero permissions
    private const byte AdminRoleId = 4; // DB row named "Admin" - Roles.Normalize() maps it to Manager too
    private const byte CompanyAdminRoleId = 5;

    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(x => new { x.RoleId, x.PermissionId });

        builder.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Permission>().WithMany().HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);

        // Best-effort mirror of today's hardcoded StudentAuthorization.cs / controller behavior, so
        // HasPermissionAsync agrees with what the app already does for every existing user. Nothing
        // existing actually calls HasPermissionAsync yet (see the RBAC plan) - only new code opts in
        // - so a small mismatch here has zero live impact until a deliberate phase-2 migration reads
        // from this table for an existing screen. SuperAdmin isn't seeded at all: it's a config-based
        // identity with no Roles row (see AGENTS.md), handled by an unconditional bypass in
        // PermissionService instead.
        var studentPermissions = new[]
        {
            Permissions.Students.ViewId,
            Permissions.CourseLibrary.ViewId,
            Permissions.Skillsoft.LaunchCourseId,
            Permissions.Skillsoft.ViewTranscriptId,
            Permissions.Skillsoft.ViewCatalogId,
            Permissions.Faq.ViewId,
            Permissions.ContactInfo.ViewId,
        };

        var managerPermissions = new[]
        {
            Permissions.Students.ViewId,
            Permissions.Students.CreateId,
            Permissions.Students.UpdateId,
            Permissions.Students.DeleteId,
            Permissions.Students.ManagePasswordId,
            Permissions.Students.ViewCredentialsId,
            Permissions.Managers.ViewId,
            Permissions.Managers.ManagePasswordId,
            Permissions.Managers.ViewCredentialsId,
            Permissions.Faq.ViewId,
            Permissions.Faq.CreateId,
            Permissions.Faq.UpdateId,
            Permissions.Faq.DeleteId,
            Permissions.ContactInfo.ViewId,
            Permissions.ContactInfo.CreateId,
            Permissions.ContactInfo.UpdateId,
            Permissions.ContactInfo.DeleteId,
            Permissions.CourseLibrary.ViewId,
            Permissions.Skillsoft.LaunchCourseId,
            Permissions.Skillsoft.ViewTranscriptId,
            Permissions.Skillsoft.ViewCatalogId,
            Permissions.Assignments.ViewId,
            Permissions.Assignments.CreateId,
            Permissions.Assignments.CancelId,
            Permissions.SkillTrax.ViewId,
            Permissions.SkillTrax.CreateId,
            Permissions.SkillTrax.DeleteId,
        };

        var companyAdminPermissions = new[]
        {
            Permissions.Students.ViewId,
            Permissions.Students.CreateId,
            Permissions.Students.UpdateId,
            Permissions.Students.DeleteId,
            Permissions.Students.ManagePasswordId,
            Permissions.Students.ViewCredentialsId,
            Permissions.Students.AssignManagerId,
            Permissions.Managers.ViewId,
            Permissions.Managers.CreateId,
            Permissions.Managers.UpdateId,
            Permissions.Managers.ManagePasswordId,
            Permissions.Managers.ViewCredentialsId,
            Permissions.Roles.ViewId,
            Permissions.Faq.ViewId,
            Permissions.Faq.CreateId,
            Permissions.Faq.UpdateId,
            Permissions.Faq.DeleteId,
            Permissions.ContactInfo.ViewId,
            Permissions.ContactInfo.CreateId,
            Permissions.ContactInfo.UpdateId,
            Permissions.ContactInfo.DeleteId,
            Permissions.CourseLibrary.ViewId,
            Permissions.CourseLibrary.PublishId,
            Permissions.Skillsoft.LaunchCourseId,
            Permissions.Skillsoft.ViewTranscriptId,
            Permissions.Skillsoft.ViewCatalogId,
            Permissions.Assignments.ViewId,
            Permissions.Assignments.CreateId,
            Permissions.Assignments.CancelId,
            Permissions.SkillTrax.ViewId,
            Permissions.SkillTrax.CreateId,
            Permissions.SkillTrax.DeleteId,
        };

        var seedRows = new List<object>();
        AddRows(seedRows, StudentRoleId, studentPermissions);
        AddRows(seedRows, ManagerRoleId, managerPermissions);
        AddRows(seedRows, AdminRoleId, managerPermissions); // "Admin" DB row normalizes to Manager too
        AddRows(seedRows, CompanyAdminRoleId, companyAdminPermissions);
        // FdmRoleId intentionally gets zero rows - dormant role, matches "unhandled by any policy".

        builder.HasData(seedRows);
    }

    private static void AddRows(List<object> rows, byte roleId, IEnumerable<int> permissionIds)
    {
        foreach (var permissionId in permissionIds)
        {
            rows.Add(new { RoleId = roleId, PermissionId = permissionId });
        }
    }
}
