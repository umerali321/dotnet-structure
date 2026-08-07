using System.Reflection;
using SkillsetsBackend.Application.Common.Interfaces;
using SkillsetsBackend.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace SkillsetsBackend.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // The following map to existing tables in the SoftSkillSet database and are read-only for
    // this phase - see AppUser/Company/Role/UserCompanyRole/UserCredential in Domain/Identity.
    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<UserCompanyRole> UserCompanyRoles => Set<UserCompanyRole>();

    public DbSet<UserCredential> UserCredentials => Set<UserCredential>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }
}
