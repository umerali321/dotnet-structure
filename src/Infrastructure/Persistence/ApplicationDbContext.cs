using System.Reflection;
using SkillsetsBackend.Application.Common.Interfaces;
using SkillsetsBackend.Domain.Assignments;
using SkillsetsBackend.Domain.Communications;
using SkillsetsBackend.Domain.CourseLibrary;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Domain.LearningTranscript;
using SkillsetsBackend.Domain.Notifications;
using SkillsetsBackend.Domain.RosterImport;
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

    public DbSet<SkillportScraperSettings> SkillportScraperSettings => Set<SkillportScraperSettings>();

    public DbSet<NotificationSettings> NotificationSettings => Set<NotificationSettings>();

    public DbSet<Faq> Faqs => Set<Faq>();

    public DbSet<SupportContact> SupportContacts => Set<SupportContact>();

    public DbSet<MainCourseCategory> MainCourseCategories => Set<MainCourseCategory>();

    public DbSet<SubCourseCategory> SubCourseCategories => Set<SubCourseCategory>();

    public DbSet<Course> Courses => Set<Course>();

    public DbSet<CourseSection> CourseSections => Set<CourseSection>();

    public DbSet<LoginActivityLog> LoginActivityLogs => Set<LoginActivityLog>();

    public DbSet<CourseTaken> CourseTakens => Set<CourseTaken>();

    public DbSet<SkillTrax> SkillTrax => Set<SkillTrax>();

    public DbSet<SkillTraxCourse> SkillTraxCourses => Set<SkillTraxCourse>();

    public DbSet<Assignment> Assignments => Set<Assignment>();

    public DbSet<AssignmentEmployee> AssignmentEmployees => Set<AssignmentEmployee>();

    public DbSet<AssignmentTitle> AssignmentTitles => Set<AssignmentTitle>();

    public DbSet<SmtpSettings> SmtpSettings => Set<SmtpSettings>();

    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();

    public DbSet<RosterImportBatch> RosterImportBatches => Set<RosterImportBatch>();

    public DbSet<RosterImportBatchRow> RosterImportBatchRows => Set<RosterImportBatchRow>();

    public DbSet<LearningTranscriptImportBatch> LearningTranscriptImportBatches => Set<LearningTranscriptImportBatch>();

    public DbSet<LearningTranscriptAsset> LearningTranscriptAssets => Set<LearningTranscriptAsset>();

    public DbSet<LearningTranscriptIdentity> LearningTranscriptIdentities => Set<LearningTranscriptIdentity>();

    public DbSet<LearningTranscriptActivity> LearningTranscriptActivities => Set<LearningTranscriptActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }
}
