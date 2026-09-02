using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsetsBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemAdminRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SuperAdmin's delegate in the admin application. Seeded with NO permissions at all: a
            // SystemAdmin can do nothing until a SuperAdmin grants it, which is the whole point of
            // the role. Granting a brand-new admin role a default set of powers would hand out
            // access nobody asked for.
            //
            // Roles are plain data rows here (not EF HasData), so this is a guarded INSERT rather
            // than a generated one. RoleId is tinyint and 1-5 are taken by the existing roles.
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = N'SystemAdmin')
                BEGIN
                    SET IDENTITY_INSERT dbo.Roles ON;
                    INSERT INTO dbo.Roles (RoleId, RoleName, IsSystemRole, IsActive)
                    VALUES (6, N'SystemAdmin', 0, 1);
                    SET IDENTITY_INSERT dbo.Roles OFF;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the role's grants first - RolePermissions references it.
            migrationBuilder.Sql(@"
                DELETE rp FROM dbo.RolePermissions rp
                JOIN dbo.Roles r ON r.RoleId = rp.RoleId
                WHERE r.RoleName = N'SystemAdmin';

                DELETE FROM dbo.Roles WHERE RoleName = N'SystemAdmin';
            ");
        }
    }
}
