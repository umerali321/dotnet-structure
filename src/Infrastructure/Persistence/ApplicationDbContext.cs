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

    // Schema is migration-ready; RefreshTokens is not yet read from/written to via EF Core.
    // IRefreshTokenRepository currently has an in-memory implementation - see
    // Infrastructure/Auth/InMemoryRefreshTokenRepository.cs.
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }
}
