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
        int page,
        int pageSize,
        string? eventType,
        string? search,
        string? companyName,
        DateOnly? startDate,
        DateOnly? endDate,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.LoginActivityLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(l => l.EventType == eventType);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(l => l.Email.Contains(search) || (l.Name != null && l.Name.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(companyName))
        {
            query = query.Where(l => l.CompanyName != null && l.CompanyName.Contains(companyName));
        }

        if (startDate.HasValue)
        {
            var start = new DateTimeOffset(startDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(l => l.CreatedAt >= start);
        }

        if (endDate.HasValue)
        {
            var end = new DateTimeOffset(endDate.Value.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
            query = query.Where(l => l.CreatedAt <= end);
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

    public async Task<IReadOnlyList<DateTimeOffset>> GetRecentFailedLoginTimestampsAsync(
        string email, DateTimeOffset sinceUtc, int maxCount, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLower();

        return await _dbContext.LoginActivityLogs
            .AsNoTracking()
            .Where(l => l.EventType == LoginActivityEventTypes.LoginFailed
                && l.Email.ToLower() == normalizedEmail
                && l.CreatedAt >= sinceUtc)
            .OrderByDescending(l => l.CreatedAt)
            .Take(maxCount)
            .Select(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
