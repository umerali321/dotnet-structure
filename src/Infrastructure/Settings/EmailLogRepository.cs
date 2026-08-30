using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Settings.DTOs;
using SkillsetsBackend.Application.Settings.Interfaces;
using SkillsetsBackend.Domain.Communications;
using SkillsetsBackend.Infrastructure.Persistence;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Infrastructure.Settings;

public class EmailLogRepository : IEmailLogRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EmailLogRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(EmailLog log, CancellationToken cancellationToken = default)
    {
        _dbContext.EmailLogs.Add(log);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PaginatedList<EmailLogDto>> ListAsync(
        int page, int pageSize, string? search, string? purpose, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.EmailLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(purpose))
        {
            var exactPurpose = purpose.Trim();
            query = query.Where(x => x.Purpose == exactPurpose);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{EscapeLike(search.Trim())}%";
            query = query.Where(x => EF.Functions.Like(x.ToAddress, term, "\\"));
        }

        query = query.OrderByDescending(x => x.SentAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new EmailLogDto(
                x.EmailLogId, x.ToAddress, x.ToName, x.Subject, x.Purpose, x.Provider, x.Success, x.ErrorMessage, x.SentAt))
            .ToListAsync(cancellationToken);

        return new PaginatedList<EmailLogDto>(items, totalCount, page, pageSize);
    }

    public async Task<EmailLogDetailDto?> GetByIdAsync(int emailLogId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EmailLogs
            .AsNoTracking()
            .Where(x => x.EmailLogId == emailLogId)
            .Select(x => new EmailLogDetailDto(
                x.EmailLogId, x.FromAddress, x.FromName, x.ToAddress, x.ToName, x.Subject, x.BodyHtml,
                x.Purpose, x.Provider, x.Success, x.ErrorMessage, x.SentAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
}
