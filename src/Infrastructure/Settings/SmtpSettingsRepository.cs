using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Settings.Interfaces;
using SkillsetsBackend.Domain.Communications;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.Settings;

public class SmtpSettingsRepository : ISmtpSettingsRepository
{
    private readonly ApplicationDbContext _dbContext;

    public SmtpSettingsRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<SmtpSettings?> GetAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SmtpSettings.FirstOrDefaultAsync(cancellationToken);

    public void Add(SmtpSettings settings) => _dbContext.SmtpSettings.Add(settings);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
