using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Faqs.DTOs;
using SkillsetsBackend.Application.Faqs.Interfaces;
using SkillsetsBackend.Domain.Support;
using SkillsetsBackend.Infrastructure.Persistence;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Infrastructure.Faqs;

public class FaqRepository : IFaqRepository
{
    private readonly ApplicationDbContext _dbContext;

    public FaqRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedList<FaqDto>> ListAsync(FaqListQueryOptions options, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Faqs.AsNoTracking().AsQueryable();

        if (options.RestrictToCompanyIds is not null)
        {
            var allowed = options.RestrictToCompanyIds;
            query = query.Where(f => f.CompanyId == null || allowed.Contains(f.CompanyId.Value));
        }

        if (options.CompanyId is not null)
        {
            query = query.Where(f => f.CompanyId == options.CompanyId.Value);
        }

        if (options.IsActive.HasValue)
        {
            query = query.Where(f => f.IsActive == options.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            var term = $"%{EscapeLike(options.Search.Trim())}%";
            query = query.Where(f => EF.Functions.Like(f.Question, term, "\\") || EF.Functions.Like(f.Answer, term, "\\"));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var page = await query
            .OrderByDescending(f => f.CreatedAt)
            .ThenBy(f => f.FaqId)
            .Skip((options.Page - 1) * options.PageSize)
            .Take(options.PageSize)
            .Select(f => new
            {
                f.FaqId,
                f.CompanyId,
                CompanyName = f.CompanyId == null ? null : _dbContext.Companies.Where(c => c.CompanyId == f.CompanyId).Select(c => c.CompanyName).FirstOrDefault(),
                f.Question,
                f.Answer,
                f.IsActive,
                f.CreatedAt,
                f.UpdatedAt,
                f.CreatedBy,
                f.UpdatedBy,
            })
            .ToListAsync(cancellationToken);

        var items = page
            .Select(f => new FaqDto(f.FaqId, f.CompanyId, f.CompanyName, f.Question, f.Answer, f.IsActive, f.CreatedAt, f.UpdatedAt, f.CreatedBy, f.UpdatedBy))
            .ToList();

        return new PaginatedList<FaqDto>(items, totalCount, options.Page, options.PageSize);
    }

    public async Task<FaqDto?> GetDtoAsync(int faqId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Faqs
            .AsNoTracking()
            .Where(f => f.FaqId == faqId)
            .Select(f => new FaqDto(
                f.FaqId,
                f.CompanyId,
                f.CompanyId == null ? null : _dbContext.Companies.Where(c => c.CompanyId == f.CompanyId).Select(c => c.CompanyName).FirstOrDefault(),
                f.Question,
                f.Answer,
                f.IsActive,
                f.CreatedAt,
                f.UpdatedAt,
                f.CreatedBy,
                f.UpdatedBy))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Faq?> GetEntityAsync(int faqId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Faqs.FirstOrDefaultAsync(f => f.FaqId == faqId, cancellationToken);
    }

    public async Task AddAsync(Faq faq, CancellationToken cancellationToken = default)
    {
        await _dbContext.Faqs.AddAsync(faq, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
}
