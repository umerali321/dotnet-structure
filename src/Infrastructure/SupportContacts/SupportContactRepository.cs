using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.SupportContacts.DTOs;
using SkillsetsBackend.Application.SupportContacts.Interfaces;
using SkillsetsBackend.Domain.Support;
using SkillsetsBackend.Infrastructure.Persistence;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Infrastructure.SupportContacts;

public class SupportContactRepository : ISupportContactRepository
{
    private readonly ApplicationDbContext _dbContext;

    public SupportContactRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedList<SupportContactDto>> ListAsync(SupportContactListQueryOptions options, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SupportContacts.AsNoTracking().AsQueryable();

        if (options.RestrictToCompanyIds is not null)
        {
            var allowed = options.RestrictToCompanyIds;
            query = query.Where(c => c.CompanyId == null || allowed.Contains(c.CompanyId.Value));
        }

        if (options.CompanyId is not null)
        {
            query = query.Where(c => c.CompanyId == options.CompanyId.Value);
        }

        if (options.IsActive.HasValue)
        {
            query = query.Where(c => c.IsActive == options.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var page = await query
            .OrderBy(c => c.SortOrder)
            .ThenByDescending(c => c.CreatedAt)
            .ThenBy(c => c.SupportContactId)
            .Skip((options.Page - 1) * options.PageSize)
            .Take(options.PageSize)
            .Select(c => new
            {
                c.SupportContactId,
                c.CompanyId,
                CompanyName = c.CompanyId == null ? null : _dbContext.Companies.Where(comp => comp.CompanyId == c.CompanyId).Select(comp => comp.CompanyName).FirstOrDefault(),
                c.ContactType,
                c.Value,
                c.SortOrder,
                c.IsActive,
                c.CreatedAt,
                c.UpdatedAt,
                c.CreatedBy,
                c.UpdatedBy,
            })
            .ToListAsync(cancellationToken);

        var items = page
            .Select(c => new SupportContactDto(c.SupportContactId, c.CompanyId, c.CompanyName, c.ContactType, c.Value, c.SortOrder, c.IsActive, c.CreatedAt, c.UpdatedAt, c.CreatedBy, c.UpdatedBy))
            .ToList();

        return new PaginatedList<SupportContactDto>(items, totalCount, options.Page, options.PageSize);
    }

    public async Task<SupportContactDto?> GetDtoAsync(int supportContactId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SupportContacts
            .AsNoTracking()
            .Where(c => c.SupportContactId == supportContactId)
            .Select(c => new SupportContactDto(
                c.SupportContactId,
                c.CompanyId,
                c.CompanyId == null ? null : _dbContext.Companies.Where(comp => comp.CompanyId == c.CompanyId).Select(comp => comp.CompanyName).FirstOrDefault(),
                c.ContactType,
                c.Value,
                c.SortOrder,
                c.IsActive,
                c.CreatedAt,
                c.UpdatedAt,
                c.CreatedBy,
                c.UpdatedBy))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SupportContact?> GetEntityAsync(int supportContactId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SupportContacts.FirstOrDefaultAsync(c => c.SupportContactId == supportContactId, cancellationToken);
    }

    public async Task AddAsync(SupportContact contact, CancellationToken cancellationToken = default)
    {
        await _dbContext.SupportContacts.AddAsync(contact, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
