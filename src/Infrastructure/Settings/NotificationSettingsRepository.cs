using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Settings.Interfaces;
using SkillsetsBackend.Domain.Notifications;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.Settings;

public class NotificationSettingsRepository : INotificationSettingsRepository
{
    private readonly ApplicationDbContext _dbContext;

    public NotificationSettingsRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<NotificationSettings?> GetAsync(CancellationToken cancellationToken = default) =>
        _dbContext.NotificationSettings.FirstOrDefaultAsync(cancellationToken);

    public void Add(NotificationSettings settings) => _dbContext.NotificationSettings.Add(settings);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
