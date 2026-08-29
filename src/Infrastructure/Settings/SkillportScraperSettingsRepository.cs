using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Settings.Interfaces;
using SkillsetsBackend.Domain.Skillsoft;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.Settings;

public class SkillportScraperSettingsRepository : ISkillportScraperSettingsRepository
{
    private readonly ApplicationDbContext _dbContext;

    public SkillportScraperSettingsRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<SkillportScraperSettings?> GetAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SkillportScraperSettings.FirstOrDefaultAsync(cancellationToken);

    public void Add(SkillportScraperSettings settings) => _dbContext.SkillportScraperSettings.Add(settings);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
