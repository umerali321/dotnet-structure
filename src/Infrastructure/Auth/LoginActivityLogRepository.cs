using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Auth.DTOs;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Infrastructure.Persistence;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Infrastructure.Auth;

public class LoginActivityLogRepository : ILoginActivityLogRepository
{
    private readonly ApplicationDbContext _dbContext;

    public LoginActivityLogRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(LoginActivityLog log, CancellationToken cancellationToken = default)
    {
        await _dbContext.LoginActivityLogs.AddAsync(log, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    public async Task<PaginatedList<LoginActivityLogDto>> ListAsync(
        int page, int pageSize, string? eventType, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.LoginActivityLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(l => l.EventType == eventType);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.LoginActivityLogId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LoginActivityLogDto(
                l.LoginActivityLogId,
                l.EventType,
                l.Email,
                l.UserId,
                l.Name,
                l.Phone,
                l.CompanyName,
                l.Message,
                l.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedList<LoginActivityLogDto>(items, totalCount, page, pageSize);
    }

    public async Task<LoginActivitySummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var counts = await _dbContext.LoginActivityLogs
            .AsNoTracking()
            .GroupBy(l => l.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int CountFor(string eventType) => counts.FirstOrDefault(c => c.EventType == eventType)?.Count ?? 0;

        return new LoginActivitySummaryDto(
            CountFor(LoginActivityEventTypes.PasswordResetSucceeded),
            CountFor(LoginActivityEventTypes.PasswordResetFailed),
            CountFor(LoginActivityEventTypes.SupportRequestSubmitted));
    }
}
