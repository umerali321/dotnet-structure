using System.Reflection;
using SkillsetsBackend.Application.Common.Interfaces;
using SkillsetsBackend.Domain.CourseLibrary;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Domain.Skillsoft;
using SkillsetsBackend.Domain.Support;
using Microsoft.EntityFrameworkCore;

namespace SkillsetsBackend.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // The following map to existing tables in the SoftSkillSet database.
    // writable for student management - see Domain/Identity.
    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<UserPermissionOverride> UserPermissionOverrides => Set<UserPermissionOverride>();

    public DbSet<UserCompanyRole> UserCompanyRoles => Set<UserCompanyRole>();

    public DbSet<UserCredential> UserCredentials => Set<UserCredential>();

    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();

    public DbSet<ActiveLibraryCard> ActiveLibraryCards => Set<ActiveLibraryCard>();

    public DbSet<SkillportSession> SkillportSessions => Set<SkillportSession>();

    public DbSet<Faq> Faqs => Set<Faq>();

    public DbSet<SupportContact> SupportContacts => Set<SupportContact>();

    public DbSet<MainCourseCategory> MainCourseCategories => Set<MainCourseCategory>();

    public DbSet<SubCourseCategory> SubCourseCategories => Set<SubCourseCategory>();

    public DbSet<Course> Courses => Set<Course>();

    public DbSet<CourseSection> CourseSections => Set<CourseSection>();

    public DbSet<LoginActivityLog> LoginActivityLogs => Set<LoginActivityLog>();

    public DbSet<CourseTaken> CourseTakens => Set<CourseTaken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }
}
