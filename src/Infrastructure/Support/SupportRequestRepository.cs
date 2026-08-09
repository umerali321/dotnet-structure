using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Support.DTOs;
using SkillsetsBackend.Application.Support.Interfaces;
using SkillsetsBackend.Domain.Support;
using SkillsetsBackend.Infrastructure.Persistence;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Infrastructure.Support;

public class SupportRequestRepository : ISupportRequestRepository
{
    private readonly ApplicationDbContext _dbContext;

    public SupportRequestRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedList<SupportRequestDto>> ListAsync(SupportRequestListQueryOptions options, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SupportRequests.AsNoTracking().AsQueryable();

        if (options.RestrictToCompanyIds is not null)
        {
            var allowed = options.RestrictToCompanyIds;
            query = query.Where(r => allowed.Contains(r.CompanyId));
        }

        if (options.RestrictToUserId is not null)
        {
            query = query.Where(r => r.UserId == options.RestrictToUserId.Value);
        }

        if (options.CompanyId is not null)
        {
            query = query.Where(r => r.CompanyId == options.CompanyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(options.Status))
        {
            query = query.Where(r => r.Status == options.Status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var page = await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenBy(r => r.SupportRequestId)
            .Skip((options.Page - 1) * options.PageSize)
            .Take(options.PageSize)
            .Select(r => new
            {
                r.SupportRequestId,
                r.CompanyId,
                CompanyName = _dbContext.Companies.Where(c => c.CompanyId == r.CompanyId).Select(c => c.CompanyName).FirstOrDefault(),
                r.UserId,
                UserEmail = _dbContext.Users.Where(u => u.UserId == r.UserId).Select(u => u.Email).FirstOrDefault(),
                r.Subject,
                r.Message,
                r.Status,
                r.CreatedAt,
                r.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        var items = page
            .Select(r => new SupportRequestDto(r.SupportRequestId, r.CompanyId, r.CompanyName, r.UserId, r.UserEmail, r.Subject, r.Message, r.Status, r.CreatedAt, r.UpdatedAt))
            .ToList();

        return new PaginatedList<SupportRequestDto>(items, totalCount, options.Page, options.PageSize);
    }

    public async Task<SupportRequestDto?> GetDtoAsync(int supportRequestId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SupportRequests
            .AsNoTracking()
            .Where(r => r.SupportRequestId == supportRequestId)
            .Select(r => new SupportRequestDto(
                r.SupportRequestId,
                r.CompanyId,
                _dbContext.Companies.Where(c => c.CompanyId == r.CompanyId).Select(c => c.CompanyName).FirstOrDefault(),
                r.UserId,
                _dbContext.Users.Where(u => u.UserId == r.UserId).Select(u => u.Email).FirstOrDefault(),
                r.Subject,
                r.Message,
                r.Status,
                r.CreatedAt,
                r.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SupportRequest?> GetEntityAsync(int supportRequestId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SupportRequests.FirstOrDefaultAsync(r => r.SupportRequestId == supportRequestId, cancellationToken);
    }

    public async Task AddAsync(SupportRequest request, CancellationToken cancellationToken = default)
    {
        await _dbContext.SupportRequests.AddAsync(request, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
